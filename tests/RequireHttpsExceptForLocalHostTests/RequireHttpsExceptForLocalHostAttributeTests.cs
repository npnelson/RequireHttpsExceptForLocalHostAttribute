using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.WebUtilities;
using RequireHttpsExceptForLocalHost;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RequireHttpsExceptForLocalHostTests
{
    public class RequireHttpsExceptForLocalHostAttributeTests
    {

        [Fact]
        public void OnAuthorization_AllowsTheRequestIfItIsHttps()
        {
            // Arrange
            var requestContext = new DefaultHttpContext();
            requestContext.Request.Scheme = "https";

            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new RequireHttpsExceptForLocalHostAttribute();

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            Assert.Null(authContext.Result);
        }

        public static IEnumerable<object[]> NonRedirectEndpointTestData
        {
            get {
                var data = new TheoryData<string,string,string,string>();

                data.Add("LOCALHOST", null, null, null);
                data.Add("localhost:5000", null, null, null);
                data.Add("localhost", "/pathbase", null, null);
                data.Add("localhost", "/pathbase", "/path", null);
                data.Add("localhost", "/pathbase", "/path", "?foo=bar");

                // Encode some special characters on the url.
                data.Add("localhost", "/path?base", null, null);
                data.Add("localhost", null, "/pa?th", null);
                data.Add("localhost", "/", null, "?foo=bar%2Fbaz");

                return data;

            }
        }

        public static IEnumerable<object[]> RedirectToHttpEndpointTestData
        {
            get
            {
                // host, pathbase, path, query, expectedRedirectUrl
                var data = new TheoryData<string, string, string, string, string>();
             
                data.Add("localhost1", null, null, null, "https://localhost1");
                data.Add("localhost1:5000", null, null, null, "https://localhost1:5000");
                data.Add("localhost1", "/pathbase", null, null, "https://localhost1/pathbase");
                data.Add("localhost1", "/pathbase", "/path", null, "https://localhost1/pathbase/path");
                data.Add("localhost1", "/pathbase", "/path", "?foo=bar", "https://localhost1/pathbase/path?foo=bar");

                data.Add("localhost.test.com", null, null, null, "https://localhost.test.com");
                data.Add("localhost.test.com:5000", null, null, null, "https://localhost.test.com:5000");

                // Encode some special characters on the url.
                data.Add("localhost1", "/path?base", null, null, "https://localhost1/path%3Fbase");
                data.Add("localhost1", null, "/pa?th", null, "https://localhost1/pa%3Fth");
                data.Add("localhost1", "/", null, "?foo=bar%2Fbaz", "https://localhost1/?foo=bar%2Fbaz");


                //this next assertion was copied from the requirehttps tests on aspnet github, decided not to support the chinese version of localhost


                //// Urls with punycode
                //// 本地主機 is "localhost" in chinese traditional, "xn--tiq21tzznx7c" is the
                //// punycode representation.
                //data.Add("本地主機", "/", null, null, "https://xn--tiq21tzznx7c/");
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(RedirectToHttpEndpointTestData))]
        public void OnAuthorization_RedirectsToHttpsEndpoint_ForNonHttps_GetRequests(
           string host,
           string pathBase,
           string path,
           string queryString,
           string expectedUrl)
        {
            // Arrange
            var requestContext = new DefaultHttpContext();
            requestContext.Request.Scheme = "http";
            requestContext.Request.Method = "GET";
            requestContext.Request.Host = HostString.FromUriComponent(host);

            if (pathBase != null)
            {
                requestContext.Request.PathBase = new PathString(pathBase);
            }

            if (path != null)
            {
                requestContext.Request.Path = new PathString(path);
            }

            if (queryString != null)
            {
                requestContext.Request.QueryString = new QueryString(queryString);
            }

            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new RequireHttpsExceptForLocalHostAttribute();

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            Assert.NotNull(authContext.Result);
            var result = Assert.IsType<RedirectResult>(authContext.Result);

            Assert.True(result.Permanent);
            Assert.Equal(expectedUrl, result.Url);
        }

        [Theory]
        [MemberData(nameof(NonRedirectEndpointTestData))]
        public void OnAuthorization_DoesNotRedirectToHttpsEndpoint_ForNonHttps_GetRequestsToLocalhost(
            string host,
            string pathBase,
            string path,
            string queryString
            )
        {
            // Arrange
            var requestContext = new DefaultHttpContext();
            requestContext.Request.Scheme = "http";
            requestContext.Request.Method = "GET";
            requestContext.Request.Host = HostString.FromUriComponent(host);

            if (pathBase != null)
            {
                requestContext.Request.PathBase = new PathString(pathBase);
            }

            if (path != null)
            {
                requestContext.Request.Path = new PathString(path);
            }

            if (queryString != null)
            {
                requestContext.Request.QueryString = new QueryString(queryString);
            }

            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new RequireHttpsExceptForLocalHostAttribute();

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            Assert.Null(authContext.Result);
        
        }


        public static IEnumerable<object[]> NonGetTestData
        { get
            {
                var data = new TheoryData<string, string, bool>();
                data.Add("POST", "localhost", true);
                data.Add("POST", "localhost1", false);
                data.Add("PUT", "localhost", true);
                data.Add("PUT", "localhost1", false);
                data.Add("PATCH", "localhost", true);
                data.Add("PATCH", "localhost1", false);
                data.Add("DELETE", "localhost", true);
                data.Add("DELETE", "localhost1", false);
                return data;

            } }
        [Theory]
        [MemberData(nameof(NonGetTestData))]
       
        public void OnAuthorization_SignalsBadRequestStatusCode_ForNonHttpsAndNonGetRequestsExceptForLocalHost(string method,string host,bool leaveAlone)
        {
            // Arrange
            var requestContext = new DefaultHttpContext();
            requestContext.Request.Scheme = "http";
            requestContext.Request.Method = method;
            requestContext.Request.Host = HostString.FromUriComponent(host);        
            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new RequireHttpsExceptForLocalHostAttribute();

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            if (leaveAlone)
            {
                Assert.Null(authContext.Result);  //we should be leaving it alone
            }
            else
            {
                Assert.NotNull(authContext.Result);
                var result = Assert.IsType<HttpStatusCodeResult>(authContext.Result);
                Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
            }
        }

        [Fact]
        public void HandleNonHttpsRequestExtensibility()
        {
            // Arrange
            var requestContext = new DefaultHttpContext();
            requestContext.Request.Scheme = "http";
            requestContext.Request.Host = HostString.FromUriComponent("testhost");

            var authContext = CreateAuthorizationContext(requestContext);
            var attr = new CustomRequireHttpsAttribute();

            // Act
            attr.OnAuthorization(authContext);

            // Assert
            var result = Assert.IsType<HttpStatusCodeResult>(authContext.Result);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        }

        private class CustomRequireHttpsAttribute : RequireHttpsExceptForLocalHostAttribute
        {
            protected override void HandleNonHttpsRequest(AuthorizationContext filterContext)
            {
                filterContext.Result = new HttpStatusCodeResult(StatusCodes.Status404NotFound);
            }
        }

        private static AuthorizationContext CreateAuthorizationContext(HttpContext ctx)
        {
            var actionContext = new ActionContext(ctx, new RouteData(), actionDescriptor: null);

            return new AuthorizationContext(actionContext, Enumerable.Empty<IFilter>().ToList());
        }


    }


  //  These were added to https://raw.githubusercontent.com/aspnet/HttpAbstractions/dev/src/Microsoft.AspNet.WebUtilities/StatusCodes.cs after beta 3,
  //  copied them here for ease of use, but they should be removed in beta 4 because the real ones will be available and this copy will no longer be needed.
    public static class StatusCodes
    {
        public const int Status200OK = 200;
        public const int Status201Created = 201;
        public const int Status202Accepted = 202;
        public const int Status203NonAuthoritative = 203;
        public const int Status204NoContent = 204;
        public const int Status205ResetContent = 205;
        public const int Status206PartialContent = 206;

        public const int Status300MultipleChoices = 300;
        public const int Status301MovedPermanently = 301;
        public const int Status302Found = 302;
        public const int Status303SeeOther = 303;
        public const int Status304NotModified = 304;
        public const int Status305UseProxy = 305;
        public const int Status306SwitchProxy = 306;
        public const int Status307TemporaryRedirect = 307;

        public const int Status400BadRequest = 400;
        public const int Status401Unauthorized = 401;
        public const int Status402PaymentRequired = 402;
        public const int Status403Forbidden = 403;
        public const int Status404NotFound = 404;
        public const int Status405MethodNotAllowed = 405;
        public const int Status406NotAcceptable = 406;
        public const int Status407ProxyAuthenticationRequired = 407;
        public const int Status408RequestTimeout = 408;
        public const int Status409Conflict = 409;
        public const int Status410Gone = 410;
        public const int Status411LengthRequired = 411;
        public const int Status412PreconditionFailed = 412;
        public const int Status413RequestEntityTooLarge = 413;
        public const int Status414RequestUriTooLong = 414;
        public const int Status415UnsupportedMediaType = 415;
        public const int Status416RequestedRangeNotSatisfiable = 416;
        public const int Status417ExpectationFailed = 417;
        public const int Status418ImATeapot = 418;
        public const int Status419AuthenticationTimeout = 419;

        public const int Status500InternalServerError = 500;
        public const int Status501NotImplemented = 501;
        public const int Status502BadGateway = 502;
        public const int Status503ServiceUnavailable = 503;
        public const int Status504GatewayTimeout = 504;
        public const int Status505HttpVersionNotsupported = 505;
        public const int Status506VariantAlsoNegotiates = 506;
    }
}

