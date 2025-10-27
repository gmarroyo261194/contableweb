abp install-libs

cd ContableWeb && dotnet run --migrate-database && cd -

cd ContableWeb.Blazor && dotnet dev-certs https -v -ep openiddict.pfx -p 80efd94a-4146-41ef-9f44-0299bea79dcf




exit 0