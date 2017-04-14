// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// https://github.com/jamesemann
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace CommunityDialogs
{

    [Serializable]
    public abstract class FacebookAccountLinkingDialog : IDialog<object>
    {
        protected string LoginUrl { get; set; }

        protected void SetFacebookAuthorizationCode(IDialogContext context, string value)
        {
            if (value == null)
            {
                if (context.UserData.ContainsKey("fb_accountlinking_authorization_code"))
                {
                    context.UserData.RemoveValue("fb_accountlinking_authorization_code");
                }
            }
            else
            {
                context.UserData.SetValue("fb_accountlinking_authorization_code", value);
            }
        }
        protected string GetFacebookAuthorizationCode(IDialogContext context)
        {
            string result = null;
            context.UserData.TryGetValue("fb_accountlinking_authorization_code", out result);
            return result;
        }

        protected string GetAlreadyLoggedInMessage(string authcode)
        {
            return $"you are already logged in {authcode}";
        }

        protected string GetLogInMessage()
        {
            return $"Log in to your account";
        }

        public FacebookAccountLinkingDialog(string loginUrl)
        {
            this.LoginUrl = loginUrl;
        }

        public async Task StartAsync(IDialogContext context)
        {
            var facebookAuthCode = GetFacebookAuthorizationCode(context);
            if (facebookAuthCode != null)
            {
                var reply = context.MakeMessage();
                var attachment = CreateLogoutPayload(GetAlreadyLoggedInMessage(facebookAuthCode), LoginUrl);
                reply.ChannelData = JObject.FromObject(new { attachment });
                await context.PostAsync(reply);
            }
            else
            {
                var reply = context.MakeMessage();
                var attachment = CreateLoginLogoutPayload(GetLogInMessage(), LoginUrl);
                reply.ChannelData = JObject.FromObject(new { attachment });
                await context.PostAsync(reply);
            }

            // State transition - wait for 'account link/unlink' message
            context.Wait(LinkedOrUninlinked);
        }

        public abstract Task Linked(IDialogContext context, IMessageActivity argument, string authorizationCode);

        public abstract Task Unlinked(IDialogContext context, IMessageActivity argument);


        public async Task LinkedOrUninlinked(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var activity = await argument;

            if (activity != null)
            {
                var jobj = JObject.FromObject(activity.ChannelData);
                var fbAccountLinking = jobj.SelectToken("$..account_linking");

                if (fbAccountLinking != null)
                {
                    var linkedUnlinked = fbAccountLinking.Value<string>("status");

                    if (linkedUnlinked == "unlinked")
                    {
                        SetFacebookAuthorizationCode(context, null);
                        await Unlinked(context, activity);
                    }
                    else
                    {
                        var authorizationCode = fbAccountLinking.Value<string>("authorization_code");
                        SetFacebookAuthorizationCode(context, authorizationCode);
                        await Linked(context, activity, authorizationCode);
                    }

                    context.Done<object>(new object());
                }
                else
                {
                    var reply = context.MakeMessage();
                    var attachment = CreateLoginLogoutPayload(GetLogInMessage(),LoginUrl);
                    reply.ChannelData = JObject.FromObject(new { attachment });

                    await context.PostAsync(reply);

                    // State transition - wait for 'account link/unlink' message
                    context.Wait(LinkedOrUninlinked);
                }
            }
        }


        private static object CreateLoginLogoutPayload(string message, string loginUrl)
        {

            var attachment = new
            {
                type = "template",
                payload = new
                {
                    template_type = "button",
                    text = message,
                    buttons = new[]
                    {
                        new
                        {
                            type = "account_link",
                            url = loginUrl
                        }
                    }
                }
            };
            return attachment;
        }

        private static object CreateLogoutPayload(string message, string loginUrl)
        {
            var attachment = new
            {
                type = "template",
                payload = new
                {
                    template_type = "button",
                    text = message,
                    buttons = new[]
                    {
                        new
                        {
                            type = "account_unlink",
                            url = ""
                        }
                    }
                }
            };
            return attachment;
        }
    }
}
