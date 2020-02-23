﻿using System.IO;
using EasyAbp.Abp.SettingUi;
using EasyAbp.Abp.SettingUi.Localization;
using EasyAbp.Abp.SettingUi.Web;
using Localization.Resources.AbpUi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyAbpApp.EntityFrameworkCore;
using MyAbpApp.Localization;
using MyAbpApp.MultiTenancy;
using MyAbpApp.Web.Menus;
using Microsoft.OpenApi.Models;
using Volo.Abp;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Authentication.JwtBearer;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Identity.Web;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement.Web;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;

namespace MyAbpApp.Web
{
    [DependsOn(
        typeof(MyAbpAppHttpApiModule),
        typeof(MyAbpAppApplicationModule),
        typeof(MyAbpAppEntityFrameworkCoreDbMigrationsModule),
        typeof(AbpAutofacModule),
        typeof(AbpIdentityWebModule),
        typeof(AbpAccountWebIdentityServerModule),
        typeof(AbpAspNetCoreMvcUiBasicThemeModule),
        typeof(AbpAspNetCoreAuthenticationJwtBearerModule),
        typeof(AbpTenantManagementWebModule),
        typeof(AbpAspNetCoreSerilogModule),
        typeof(SettingUiWebModule)
    )]
    public class MyAbpAppWebModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
            {
                options.AddAssemblyResource(
                    typeof(MyAbpAppResource),
                    typeof(MyAbpAppDomainModule).Assembly,
                    typeof(MyAbpAppDomainSharedModule).Assembly,
                    typeof(MyAbpAppApplicationModule).Assembly,
                    typeof(MyAbpAppApplicationContractsModule).Assembly,
                    typeof(MyAbpAppWebModule).Assembly
                );
            });
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var hostingEnvironment = context.Services.GetHostingEnvironment();
            var configuration = context.Services.GetConfiguration();

            ConfigureUrls(configuration);
            ConfigureAuthentication(context, configuration);
            ConfigureAutoMapper();
            ConfigureVirtualFileSystem(hostingEnvironment);
            ConfigureLocalizationServices();
            ConfigureNavigationServices();
            ConfigureAutoApiControllers();
            ConfigureSwaggerServices(context.Services);
        }

        private void ConfigureUrls(IConfiguration configuration)
        {
            Configure<AppUrlOptions>(options => { options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"]; });
        }

        private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddAuthentication()
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = configuration["AuthServer:Authority"];
                    options.RequireHttpsMetadata = false;
                    options.ApiName = "MyAbpApp";
                });
        }

        private void ConfigureAutoMapper()
        {
            Configure<AbpAutoMapperOptions>(options => { options.AddMaps<MyAbpAppWebModule>(); });
        }

        private void ConfigureVirtualFileSystem(IWebHostEnvironment hostingEnvironment)
        {
            if (hostingEnvironment.IsDevelopment())
            {
                Configure<AbpVirtualFileSystemOptions>(options =>
                {
                    char sept = Path.DirectorySeparatorChar;
                    options.FileSets.ReplaceEmbeddedByPhysical<MyAbpAppDomainSharedModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{sept}MyAbpApp.Domain.Shared"));
                    options.FileSets.ReplaceEmbeddedByPhysical<MyAbpAppDomainModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{sept}MyAbpApp.Domain"));
                    options.FileSets.ReplaceEmbeddedByPhysical<MyAbpAppApplicationContractsModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{sept}MyAbpApp.Application.Contracts"));
                    options.FileSets.ReplaceEmbeddedByPhysical<MyAbpAppApplicationModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{sept}MyAbpApp.Application"));
                    options.FileSets.ReplaceEmbeddedByPhysical<MyAbpAppWebModule>(hostingEnvironment.ContentRootPath);
                    options.FileSets.ReplaceEmbeddedByPhysical<SettingUiWebModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{sept}..{sept}..{sept}..{sept}src{sept}EasyAbp.Abp.SettingUi.Web"));
                });
            }
        }

        private void ConfigureLocalizationServices()
        {
            Configure<AbpLocalizationOptions>(options =>
            {
                options.Resources
                    .Get<MyAbpAppResource>()
                    .AddBaseTypes(
                        typeof(AbpUiResource)
                    );

                options.Resources
                    .Get<SettingUiResource>()
                    .AddVirtualJson("/Localization/MyAbpApp")
                    ;

                options.Languages.Add(new LanguageInfo("cs", "cs", "Čeština"));
                options.Languages.Add(new LanguageInfo("en", "en", "English"));
                options.Languages.Add(new LanguageInfo("pt-BR", "pt-BR", "Português"));
                options.Languages.Add(new LanguageInfo("tr", "tr", "Türkçe"));
                options.Languages.Add(new LanguageInfo("zh-Hans", "zh-Hans", "简体中文"));
                options.Languages.Add(new LanguageInfo("zh-Hant", "zh-Hant", "繁體中文"));
            });
        }

        private void ConfigureNavigationServices()
        {
            Configure<AbpNavigationOptions>(options => { options.MenuContributors.Add(new MyAbpAppMenuContributor()); });
        }

        private void ConfigureAutoApiControllers()
        {
            Configure<AbpAspNetCoreMvcOptions>(options =>
            {
                options.ConventionalControllers.Create(typeof(MyAbpAppApplicationModule).Assembly);
                options.ConventionalControllers.Create(typeof(SettingUiApplicationModule).Assembly);
            });
        }

        private void ConfigureSwaggerServices(IServiceCollection services)
        {
            services.AddSwaggerGen(
                options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo {Title = "MyAbpApp API", Version = "v1"});
                    options.DocInclusionPredicate((docName, description) => true);
                    options.CustomSchemaIds(type => type.FullName);
                }
            );
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            var env = context.GetEnvironment();

            app.UseCorrelationId();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseErrorPage();
            }

            app.UseVirtualFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseJwtTokenMiddleware();

            if (MultiTenancyConsts.IsEnabled)
            {
                app.UseMultiTenancy();
            }

            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseAbpRequestLocalization();
            app.UseSwagger();
            app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyAbpApp API"); });
            app.UseAuditing();
            app.UseAbpSerilogEnrichers();
            app.UseMvcWithDefaultRouteAndArea();
        }
    }
}