using DevExpress.AspNetCore;
using DevExpress.DashboardAspNetCore;
using DevExpress.DashboardCommon;
using DevExpress.DashboardWeb;
using DevExpress.DataAccess.Excel;
using DevExpress.DataAccess.Sql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;

namespace asp_net_core_dashboard_project {
    public class Startup {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment) {
            Configuration = configuration;
            FileProvider = hostingEnvironment.ContentRootFileProvider;
        }

        public IFileProvider FileProvider { get; }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services
                .AddResponseCompression()
                .AddDevExpressControls()
                .AddControllers();
            services.AddScoped((IServiceProvider serviceProvider) => {
                DashboardConfigurator configurator = new DashboardConfigurator();
                configurator.SetConnectionStringsProvider(new DashboardConnectionStringsProvider(Configuration));

                DashboardFileStorage dashboardFileStorage = new DashboardFileStorage(FileProvider.GetFileInfo("Data/Dashboards").PhysicalPath);
                configurator.SetDashboardStorage(dashboardFileStorage);

                DataSourceInMemoryStorage dataSourceStorage = new DataSourceInMemoryStorage();

                // Registers an SQL data source.
                DashboardSqlDataSource sqlDataSource = new DashboardSqlDataSource("SQL Data Source", "NWindConnectionString");
                sqlDataSource.DataProcessingMode = DataProcessingMode.Client;
                SelectQuery query = SelectQueryFluentBuilder
                    .AddTable("Categories").SelectAllColumnsFromTable()
                    .Join("Products", "CategoryID").SelectAllColumnsFromTable()
                    .Build("Products_Categories");
                sqlDataSource.Queries.Add(query);
                dataSourceStorage.RegisterDataSource("sqlDataSource", sqlDataSource.SaveToXml());

                // Registers an Object data source.
                DashboardObjectDataSource objDataSource = new DashboardObjectDataSource("Object Data Source");
                dataSourceStorage.RegisterDataSource("objDataSource", objDataSource.SaveToXml());

                // Registers an Excel data source.
                DashboardExcelDataSource excelDataSource = new DashboardExcelDataSource("Excel Data Source");
                excelDataSource.FileName = FileProvider.GetFileInfo("Data/Sales.xlsx").PhysicalPath;
                excelDataSource.SourceOptions = new ExcelSourceOptions(new ExcelWorksheetSettings("Sheet1"));
                dataSourceStorage.RegisterDataSource("excelDataSource", excelDataSource.SaveToXml());

                configurator.SetDataSourceStorage(dataSourceStorage);

                configurator.DataLoading += (s, e) => {
                    if(e.DataSourceName == "Object Data Source") {
                        e.Data = Invoices.CreateData();
                    }
                };
                return configurator;
            });

            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if(env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseDevExpressControls();
            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapDashboardRoute("dashboardControl", "DefaultDashboard");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}