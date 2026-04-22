using System;
using System.Linq;
using Infrastructure.Services.KLEDataBridge;
using Xunit;

namespace Tests.Integration.Presentation.Web.KLE
{
    public class KLEDataBridgeTest
    {
        [Fact]
        public void GetKLEXMLData_Returns_Valid_XML()
        {
            var sut = new KLEDataBridge("http://api.kle-online.dk/resources/kle");
            var result = sut.GetAllActiveKleNumbers();
            var publishingDateXElement = result.Descendants("UdgivelsesDato");
            DateTime.Parse(publishingDateXElement.First().Value);
        }
    }
}
