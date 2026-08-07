using System;
using System.IO;
using System.Text;
using System.Xml;
using Spectre.Console;

namespace Tools.SAMLResponseDecryptor
{
    internal class Program
    {
        private static void Main()
        {
            AnsiConsole.Write(new Rule("[bold deepskyblue1]SAML Response Decryptor[/]").RuleStyle("deepskyblue1"));
            AnsiConsole.WriteLine();

            string certPath;
            while (true)
            {
                certPath = AskPath("[deepskyblue1]Certificate file[/] [grey](PFX path, no quotes):[/]");
                if (File.Exists(certPath)) break;
                AnsiConsole.MarkupLine("[red]File not found, try again.[/]");
            }

            var certPassword = AnsiConsole.Prompt(
                new TextPrompt<string>("[deepskyblue1]Certificate password[/] [grey](from 1Password):[/]")
                    .Secret());

            AnsiConsole.WriteLine();
            AnsiConsole.Write(
                new Panel(
                    "1. Open [bold]DevTools → Network[/] tab in your browser [grey](F12)[/] and start a new recording\n" +
                    "2. Trigger the KOMBIT SSO login flow for the KITOS application\n" +
                    "3. Locate the [bold]POST[/] request to [bold]Login.ashx[/] [grey](this is the SSO callback sent by KOMBIT/n2adgangsstyring back to your SP)[/]\n" +
                    "4. Open the [bold]Payload[/] tab and copy the [bold]SAMLResponse[/] form field value\n" +
                    "5. URL-decode the value, then Base64-decode it — save the resulting XML as a [bold].xml[/] file")
                .Header("[bold yellow] How to capture the SAML response [/]")
                .BorderColor(Color.Yellow)
                .Padding(new Padding(1, 0, 1, 0)));
            AnsiConsole.WriteLine();

            var samlFilePath = AskPath("[deepskyblue1]SAML response XML file[/] [grey](path, or Enter for manual input):[/]");

            string key, data, assertionAlgorithm = string.Empty;

            if (!string.IsNullOrEmpty(samlFilePath))
            {
                if (!File.Exists(samlFilePath))
                {
                    AnsiConsole.MarkupLine($"[red]File not found:[/] {Markup.Escape(samlFilePath)}");
                    return;
                }
                if (!ExtractCipherValuesFromSamlFile(samlFilePath, out key, out data, out assertionAlgorithm))
                    return;
            }
            else
            {
                key  = AskValue("[deepskyblue1]Encrypted symmetric key[/] [grey](<e:CipherValue>, or file:path):[/]");
                data = AskValue("[deepskyblue1]Assertion cipher value[/] [grey](<xenc:CipherValue>, or file:path):[/]");
            }

            byte[] decryptedSymmetricKey;
            try
            {
                decryptedSymmetricKey = SAMLDecryptor.DecryptSymmetricKey(key, certPath, certPassword);
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                AnsiConsole.MarkupLine($"[red]Could not decrypt symmetric key:[/] {Markup.Escape(ex.Message)}");
                AnsiConsole.MarkupLine("[grey]Possible causes: wrong certificate, invalid PFX password, or all RSA padding modes failed.[/]");
                return;
            }

            string decryptedAssertion;
            try
            {
                decryptedAssertion = SAMLDecryptor.DecryptAssertion(data, decryptedSymmetricKey,
                    string.IsNullOrEmpty(assertionAlgorithm) ? null : assertionAlgorithm);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Could not decrypt assertion:[/] {Markup.Escape(ex.Message)}");
                return;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[bold green]Decrypted Assertion[/]").RuleStyle("green"));
            AnsiConsole.WriteLine(decryptedAssertion);
            AnsiConsole.Write(new Rule().RuleStyle("green"));
            AnsiConsole.WriteLine();

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(decryptedAssertion);
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");

            var node = xmlDoc.SelectSingleNode(
                "//saml:Attribute[@Name='dk:gov:saml:attribute:Privileges_intermediate']/saml:AttributeValue",
                nsmgr);

            if (node == null)
            {
                AnsiConsole.MarkupLine("[yellow]No Privileges_intermediate attribute found in the assertion.[/]");
            }
            else
            {
                var decodedPrivilege = Encoding.UTF8.GetString(Convert.FromBase64String(node.InnerText));
                AnsiConsole.Write(new Rule("[bold green]Privileges[/]").RuleStyle("green"));
                AnsiConsole.WriteLine(decodedPrivilege);
                AnsiConsole.Write(new Rule().RuleStyle("green"));
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Markup("[grey]Press Enter to exit.[/] ");
            Console.ReadLine();
        }

        private static bool ExtractCipherValuesFromSamlFile(string samlFilePath,
            out string encryptedKeyCipherValue, out string assertionCipherValue, out string assertionEncryptionAlgorithm)
        {
            encryptedKeyCipherValue      = string.Empty;
            assertionCipherValue         = string.Empty;
            assertionEncryptionAlgorithm = string.Empty;

            var xml = new XmlDocument();
            xml.Load(samlFilePath);

            var ns = new XmlNamespaceManager(xml.NameTable);
            ns.AddNamespace("xenc", "http://www.w3.org/2001/04/xmlenc#");

            // First <xenc:CipherValue> is inside <e:EncryptedKey> — the RSA-encrypted symmetric key.
            // Second <xenc:CipherValue> is inside <xenc:EncryptedData> — the AES-encrypted assertion.
            var encMethodNode = xml.SelectSingleNode("//xenc:EncryptedData/xenc:EncryptionMethod", ns);
            assertionEncryptionAlgorithm = encMethodNode?.Attributes?["Algorithm"]?.Value ?? string.Empty;

            var cipherValues = xml.SelectNodes("//xenc:CipherData/xenc:CipherValue", ns);
            if (cipherValues == null || cipherValues.Count < 2)
            {
                AnsiConsole.MarkupLine($"[red]Expected 2 CipherValue nodes, found {cipherValues?.Count ?? 0}.[/]");
                AnsiConsole.MarkupLine("[grey]Ensure the file contains a full EncryptedAssertion with both EncryptedKey and EncryptedData.[/]");
                return false;
            }

            encryptedKeyCipherValue = cipherValues[0]!.InnerText.Trim();
            assertionCipherValue    = cipherValues[1]!.InnerText.Trim();

            var algoShort = assertionEncryptionAlgorithm.Contains("gcm", StringComparison.OrdinalIgnoreCase) ? "AES-256-GCM" :
                            assertionEncryptionAlgorithm.Contains("cbc", StringComparison.OrdinalIgnoreCase) ? "AES-256-CBC" :
                            assertionEncryptionAlgorithm;

            AnsiConsole.MarkupLine(
                $"[grey]Algorithm:[/] [white]{Markup.Escape(algoShort)}[/]  " +
                $"[grey]key:[/] {encryptedKeyCipherValue.Length} chars  " +
                $"[grey]assertion:[/] {assertionCipherValue.Length} chars");
            return true;
        }

        /// <summary>Prints a coloured label and reads the raw typed path (no file-content resolution).</summary>
        private static string AskPath(string markupLabel)
        {
            AnsiConsole.Markup(markupLabel + " ");
            return Console.ReadLine()?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Prints a coloured label and reads a value that may optionally be a <c>file:</c>-prefixed path
        /// or a bare file path, in which case the file's text contents are returned instead.
        /// </summary>
        private static string AskValue(string markupLabel)
        {
            AnsiConsole.Markup(markupLabel + " ");
            var input = Console.ReadLine()?.Trim() ?? string.Empty;
            return ResolveFile(input);
        }

        private static string ResolveFile(string input)
        {
            if (input.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                var filePath = input["file:".Length..].Trim();
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");
                return File.ReadAllText(filePath).Trim();
            }
            if (File.Exists(input))
                return File.ReadAllText(input).Trim();
            return input;
        }
    }
}