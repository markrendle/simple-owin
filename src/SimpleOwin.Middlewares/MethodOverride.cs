﻿namespace SimpleOwin.Middlewares
{
    using System;
    using System.Collections.Generic;
    using SimpleOwin.Extensions;

    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    public class MethodOverride
    {
        public static Func<AppFunc, AppFunc> Middleware(string key = "_method", bool checkQuerystring = false, bool checkBody = true)
        {
            return
                next =>
                env =>
                {
                    string originalMethod = env.GetOwinRequestMethod();

                    var method = env.GetOwinRequestHeaderValue("x-http-method-override");

                    if (string.IsNullOrWhiteSpace(method))
                    {
                        // try checking body, requires JsonBodyParser/UrlEncoded middleware
                        if (checkBody)
                        {
                            var body = env.GetOwinEnvironmentValue<Lazy<object>>("simpleOwin.body");
                            if (body != null)
                            {
                                try
                                {
                                    var dictStringObject = body.Value as IDictionary<string, object>;
                                    if (dictStringObject == null)
                                    {
                                        var dictStringStringArray = body.Value as IDictionary<string, string[]>;
                                        string[] tempValue;
                                        if (dictStringStringArray != null && dictStringStringArray.TryGetValue(key, out tempValue))
                                        {
                                            if (tempValue != null && tempValue.Length == 1)
                                            {
                                                var stringTempValue = tempValue[0];
                                                if (!string.IsNullOrWhiteSpace(stringTempValue))
                                                    method = stringTempValue;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        object tempValue;
                                        if (dictStringObject.TryGetValue(key, out tempValue))
                                        {
                                            var stringTempValue = tempValue as string;
                                            if (!string.IsNullOrWhiteSpace(stringTempValue))
                                                method = stringTempValue;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // do nothing, parsing json failed
                                }
                            }
                        }

                        // try checking querystring, requires QueryParser middleware
                        if (checkQuerystring && string.IsNullOrWhiteSpace(method))
                        {
                            var qs = env.GetOwinEnvironmentValue<Lazy<IDictionary<string, string[]>>>("simpleOwin.query");
                            if (qs != null)
                                method = qs.Value.GetOwinHeaderValue(key);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(method))
                    {
                        env.SetOwinRequestMethod(method.ToUpperInvariant());
                        env["simpleOwin.originalRequestMethod"] = originalMethod;
                    }

                    return next(env);
                };
        }
    }
}