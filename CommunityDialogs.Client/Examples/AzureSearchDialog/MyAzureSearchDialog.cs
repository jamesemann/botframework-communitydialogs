﻿// 
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
using System.Configuration;

namespace CommunityDialogs.Client.Examples.AzureSearchDialog
{
    [Serializable]
    public class MyAzureSearchDialog : AzureSearchDialog<object>
    {
        public MyAzureSearchDialog() : base(
            ConfigurationManager.AppSettings["CommunityDialogs_AzureSearchDialog_SearchKey"],
            ConfigurationManager.AppSettings["CommunityDialogs_AzureSearchDialog_SearchName"],
            ConfigurationManager.AppSettings["CommunityDialogs_AzureSearchDialog_IndexName"],
            new string[] { ConfigurationManager.AppSettings["CommunityDialogs_AzureSearchDialog_Facet"] })
        {

        }

        public override string ProcessResult(dynamic result)
        {
            return $"- {result.businessname} {Environment.NewLine}";
        }
    }
}