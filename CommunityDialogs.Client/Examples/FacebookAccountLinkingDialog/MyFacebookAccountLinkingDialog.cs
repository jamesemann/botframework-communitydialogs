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

using System;
using CommunityDialogs;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using System.Configuration;

namespace CommunityDialogs.Client.Examples.FacebookAccountLinkingDialog
{
    [Serializable]
    public class MyFacebookAccountLinkingDialog : CommunityDialogs.FacebookAccountLinkingDialog
    {
        public MyFacebookAccountLinkingDialog() : base( ConfigurationManager.AppSettings["CommunityDialogs_FacebookAccountLinkingDialog_WebLoginUrl"])
        {
        }

        public override async Task Linked(IDialogContext context, IMessageActivity argument, string authorizationCode)
        {
            var reply = context.MakeMessage();

            reply.Text = $"dialog: user just linked! authorization code: {authorizationCode}";

            await context.PostAsync(reply);
        }

        public override async Task Unlinked(IDialogContext context, IMessageActivity argument)
        {
            var reply = context.MakeMessage();

            reply.Text = "dialog: user just unlinked!";

            await context.PostAsync(reply);
        }
    }
}