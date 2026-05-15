$env:ConnectionStrings__KitosContext="Server=.\SQLEXPRESS;Integrated Security=true;Initial Catalog=Kitos;MultipleActiveResultSets=True;TrustServerCertificate=True"
dotnet ef database update --project ../Infrastructure.DataAccess --startup-project ../Presentation.Web
