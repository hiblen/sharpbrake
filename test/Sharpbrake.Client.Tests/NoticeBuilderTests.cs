﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sharpbrake.Client.Tests.Mocks;
using Xunit;
#if NET35
using Xunit.Extensions;
#endif

namespace Sharpbrake.Client.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="NoticeBuilder"/> class.
    /// </summary>
    public class NoticeBuilderTests
    {
        [Fact]
        public void SetErrorEntries_ShouldSetExceptionsInCorrectOrder()
        {
            var ex = new Exception("Main exception",
                        new Exception("Inner exception 1",
                            new Exception("Inner exception 2")));

            var builder = new NoticeBuilder();
            builder.SetErrorEntries(ex);

            var errorEntries = builder.ToNotice().Errors;

            Assert.True(errorEntries.Count.Equals(3));
            Assert.True(errorEntries[0].Message.Equals("Exception: Main exception"));
            Assert.True(errorEntries[1].Message.Equals("Exception: Inner exception 1"));
            Assert.True(errorEntries[2].Message.Equals("Exception: Inner exception 2"));
        }

        [Fact]
        public void SetErrorEntries_ShouldLimitInnerExceptionsToThree()
        {
            var ex = new Exception("Main exception",
                        new Exception("Inner exception 1",
                            new Exception("Inner exception 2",
                                new Exception("Inner exception 3",
                                    new Exception("Inner exception 4")))));

            var builder = new NoticeBuilder();
            builder.SetErrorEntries(ex);

            var errorEntries = builder.ToNotice().Errors;

            // main exception + no more than 3 inner exceptions
            Assert.True(errorEntries.Count.Equals(4));
        }

        [Fact]
        public void SetErrorEntries_ShouldSetErrorMessageFromExceptionMessageIfPresent()
        {
            var ex = new FakeException("error message");

            var builder = new NoticeBuilder();
            builder.SetErrorEntries(ex);

            var errorEntries = builder.ToNotice().Errors;

            Assert.NotNull(errorEntries);
            Assert.True(errorEntries.Count == 1);
            Assert.True(errorEntries.First().Message.Equals("FakeException: error message"));
        }

        [Fact]
        public void SetErrorEntries_ShouldSetErrorMessageFromExceptionTypeIfNoExceptionMessage()
        {
            var ex = new FakeException();

            var builder = new NoticeBuilder();
            builder.SetErrorEntries(ex);

            var errorEntries = builder.ToNotice().Errors;

            Assert.NotNull(errorEntries);
            Assert.True(errorEntries.Count == 1);
            Assert.True(errorEntries.First().Message.Equals("FakeException"));
        }

        [Fact]
        public void SetErrorEntries_ShouldSetExceptionProperty()
        {
            var builder = new NoticeBuilder();
            builder.SetErrorEntries(new Exception());

            var notice = builder.ToNotice();

            Assert.NotNull(notice.Exception);
        }

        [Theory,
         InlineData("", "", ""),
         InlineData("host", "", ""),
         InlineData("host", "os", ""),
         InlineData("host", "os", "lang")]
        public void SetEnvironmentContext_ShouldSetEnvironmentContextAccordingToPassedParameters(string host, string os, string lang)
        {
            var builder = new NoticeBuilder();
            builder.SetEnvironmentContext(host, os, lang);

            var notice = builder.ToNotice();

            if (string.IsNullOrEmpty(host) && string.IsNullOrEmpty(os) && string.IsNullOrEmpty(lang))
                Assert.True(notice.Context == null);
            else
            {
                Assert.True(notice.Context != null);
                Assert.True(string.IsNullOrEmpty(host) ? string.IsNullOrEmpty(notice.Context.Hostname) : notice.Context.Hostname.Equals(host));
                Assert.True(string.IsNullOrEmpty(os) ? string.IsNullOrEmpty(notice.Context.Os) : notice.Context.Os.Equals(os));
                Assert.True(string.IsNullOrEmpty(lang) ? string.IsNullOrEmpty(notice.Context.Language) : notice.Context.Language.Equals(lang));
            }
        }

        [Fact]
        public void SetHttpContext_ShouldNotSetHttpContextIfContextParamIsEmpty()
        {
            var builder = new NoticeBuilder();
            builder.SetHttpContext(null, null);

            var notice = builder.ToNotice();

            Assert.True(notice.Context == null);
        }

        [Theory,
         InlineData("", ""),
         InlineData("url", ""),
         InlineData("url", "userAgent")]
        public void SetHttpContext_ContextHttpParameters(string url, string userAgent)
        {
            var httpContext = new FakeHttpContext
            {
                Url = url,
                UserAgent = userAgent
            };

            var builder = new NoticeBuilder();
            builder.SetHttpContext(httpContext, null);

            var notice = builder.ToNotice();

            Assert.NotNull(notice.Context);
            Assert.True(string.IsNullOrEmpty(url) ? string.IsNullOrEmpty(notice.Context.Url) : notice.Context.Url.Equals(url));
            Assert.True(string.IsNullOrEmpty(userAgent) ? string.IsNullOrEmpty(notice.Context.UserAgent) : notice.Context.UserAgent.Equals(userAgent));
        }

        [Theory,
         InlineData("", "", ""),
         InlineData("id", "", ""),
         InlineData("id", "name", ""),
         InlineData("id", "name", "email")]
        public void SetHttpContext_ContextUserInfo(string id, string name, string email)
        {
            var httpContext = new FakeHttpContext
            {
                UserName = name,
                UserId = id,
                UserEmail = email
            };

            var builder = new NoticeBuilder();
            builder.SetHttpContext(httpContext, null);

            var notice = builder.ToNotice();

            Assert.NotNull(notice.Context);
            Assert.NotNull(notice.Context.User);
            Assert.True(string.IsNullOrEmpty(id) ? string.IsNullOrEmpty(notice.Context.User.Id) : notice.Context.User.Id.Equals(id));
            Assert.True(string.IsNullOrEmpty(name) ? string.IsNullOrEmpty(notice.Context.User.Name) : notice.Context.User.Name.Equals(name));
            Assert.True(string.IsNullOrEmpty(email) ? string.IsNullOrEmpty(notice.Context.User.Email) : notice.Context.User.Email.Equals(email));
        }

        [Fact]
        public void SetHttpContext_ShouldSetParametersIfConfigIsNotDefined()
        {
            var httpContext = new FakeHttpContext
            {
                Parameters = new Dictionary<string, string>(),
                EnvironmentVars = new Dictionary<string, string>(),
                Session = new Dictionary<string, string>()
            };

            var builder = new NoticeBuilder();
            builder.SetHttpContext(httpContext, null);

            var notice = builder.ToNotice();

            Assert.NotNull(notice.Params);
            Assert.NotNull(notice.EnvironmentVars);
            Assert.NotNull(notice.Session);
        }

        [Fact]
        public void SetHttpContext_ShouldSetParametersIfConfigIsDefined()
        {
            var httpContext = new FakeHttpContext
            {
                Parameters = new Dictionary<string, string>(),
                EnvironmentVars = new Dictionary<string, string>(),
                Session = new Dictionary<string, string>()
            };

            var config = new AirbrakeConfig();

            var builder = new NoticeBuilder();
            builder.SetHttpContext(httpContext, config);

            var notice = builder.ToNotice();

            Assert.NotNull(notice.Params);
            Assert.NotNull(notice.EnvironmentVars);
            Assert.NotNull(notice.Session);
        }

        [Fact]
        public void SetHttpContext_ShouldSetHttpContextProperty()
        {
            var builder = new NoticeBuilder();
            builder.SetHttpContext(new FakeHttpContext(), null);

            var notice = builder.ToNotice();

            Assert.NotNull(notice.HttpContext);
        }

        [Fact]
        public void ToJsonString()
        {
            var builder = new NoticeBuilder();
            builder.SetErrorEntries(new Exception());

            var notice = builder.ToNotice();

            var actualJson = NoticeBuilder.ToJsonString(notice);
            var expectedJson = JsonConvert.SerializeObject(notice, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            Assert.True(actualJson.Equals(expectedJson));
        }

        [Fact]
        public void ToJsonString_ShouldNotSerializeExceptionAndHttpContext()
        {
            var builder = new NoticeBuilder();

            builder.SetErrorEntries(new Exception());
            builder.SetHttpContext(new FakeHttpContext(), null);

            var notice = builder.ToNotice();
            var json = NoticeBuilder.ToJsonString(notice);

            Assert.True(json.IndexOf("\"Exception\":", StringComparison.OrdinalIgnoreCase) == -1);
            Assert.True(json.IndexOf("\"HttpContext\":", StringComparison.OrdinalIgnoreCase) == -1);
        }

        [Fact]
        public void FromJsonString()
        {
            const string json = "{\"errors\":[{\"type\":\"System.Exception\",\"message\":\"Exception: Exception of type 'System.Exception' was thrown.\"}]}";

            var notice = NoticeBuilder.FromJsonString(json);

            Assert.NotNull(notice);
            Assert.NotNull(notice.Errors);
            Assert.NotNull(notice.Errors.Count);
        }
    }
}
