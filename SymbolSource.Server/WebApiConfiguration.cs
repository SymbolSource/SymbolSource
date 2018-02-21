using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using System.Web.Http.Tracing;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData;
using SymbolSource.Contract;
using SymbolSource.Contract.Container;
using SymbolSource.Contract.Processor;
using SymbolSource.Contract.Security;
using SymbolSource.Contract.Storage;
using SymbolSource.Server.Authentication;
using SymbolSource.Support;

namespace SymbolSource.Server
{
    public static class WebApiConfiguration
    {
        public static void Register(HttpConfiguration httpConfiguration)
        {
            Register(httpConfiguration, new DefaultConfigurationService(), null);
        }

        public static void Register(HttpConfiguration httpConfiguration, IConfigurationService configuration, Action<ContainerBuilder> register)
        {
            //var tracing = httpConfiguration.EnableSystemDiagnosticsTracing();
            //tracing.IsVerbose = true;
            //tracing.MinimumLevel = TraceLevel.Debug;

            httpConfiguration.MapHttpAttributeRoutes();

            httpConfiguration.Formatters.Add(new BinaryMediaTypeFormatter());

            RegisterDependencies(httpConfiguration, configuration, register);
            RegisterOData(httpConfiguration);
        }

        private static void RegisterDependencies(HttpConfiguration httpConfiguration, IConfigurationService configuration, Action<ContainerBuilder> register)
        {
            var builder = new ContainerBuilder();
            DefaultContainerBuilder.Register(builder, configuration);
            SupportContainerBuilder.Register(builder, SupportEnvironment.WebApp);

            builder.RegisterType<NullPackageProcessor>()
                .As<IPackageProcessor>();

            builder.RegisterType<DataController>();
            builder.RegisterType<PackagesController>();

            builder.RegisterWebApiFilterProvider(httpConfiguration);

            builder.RegisterType<AuthenticationFilter>()
                .WithParameter(TypedParameter.From<AuthenticatedAreaAttribute>(null))
                .AsWebApiAuthenticationFilterFor<DataController>();

            //var metadataAreaAttribute = new AuthenticatedAreaAttribute(
            //   AuthenticatedArea.None, "feedName");

            //builder.RegisterType<AuthenticationFilter>()
            //    .WithParameter(TypedParameter.From(metadataAreaAttribute))
            //    .AsWebApiAuthenticationFilterFor<ODataMetadataController>();

            var queryingAreaAttribute = new AuthenticatedAreaAttribute(
                AuthenticatedArea.Querying, "feedName");

            builder.RegisterType<AuthenticationFilter>()
                .WithParameter(TypedParameter.From(queryingAreaAttribute))
                .AsWebApiAuthenticationFilterFor<ODataController>();

            builder.RegisterType<AuthenticationFilter>()
                .WithParameter(TypedParameter.From(queryingAreaAttribute))
                .AsWebApiAuthenticationFilterFor<PackagesController>();

            builder.RegisterType<AuthenticationNoneFilter>()
               .Keyed<IAutofacAuthenticationFilter>(AuthenticationMode.None);

            builder.RegisterType<AuthenticationBasicFilter>()
                .Keyed<IAutofacAuthenticationFilter>(AuthenticationMode.Basic);

            builder.RegisterType<AuthenticationNuGetApiKeyFilter>()
               .Keyed<IAutofacAuthenticationFilter>(AuthenticationMode.NuGetApiKey);

            register?.Invoke(builder);

            var container = builder.Build();
            httpConfiguration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private static void RegisterOData(HttpConfiguration httpConfiguration)
        {
            httpConfiguration.AddODataQueryFilter();

            var builder = new ODataConventionModelBuilder();
            var packages = builder.EntitySet<V2FeedPackage>("Packages");

            var search = builder.Action("Search");

            search.ReturnsCollectionFromEntitySet<V2FeedPackage>("Packages");
            search.Parameter<string>("searchTerm");

            var model = builder.GetEdmModel();
            var packageType = model.FindDeclaredType(typeof(V2FeedPackage).FullName);
            model.SetHasDefaultStream((IEdmEntityType)packageType, true);

            var handler = new DefaultODataPathHandler();
            var conventions = ODataRoutingConventions.CreateDefault();
            conventions.Insert(0, new NonBindableActionRoutingConvention("Packages"));

            httpConfiguration.Routes.MapODataServiceRoute("ODataDefault", "", model, handler, conventions);
            httpConfiguration.Routes.MapODataServiceRoute("ODataNamed", "{feedName}", model, handler, conventions);
        }
    }

    public class NonBindableActionRoutingConvention : IODataRoutingConvention
    {
        private readonly string controllerName;

        public NonBindableActionRoutingConvention(string controllerName)
        {
            this.controllerName = controllerName;
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            if (odataPath.PathTemplate == "~/action")
                return controllerName;

            return null;
        }

        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            if (odataPath.PathTemplate == "~/action")
            {
                var actionSegment = odataPath.Segments.First() as ActionPathSegment;

                if (!actionSegment.Action.IsBindable && actionMap.Contains(actionSegment.Action.Name))
                    return actionSegment.Action.Name;
            }

            return null;
        }
    }
}