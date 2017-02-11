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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace CommunityDialogs
{
    [Serializable]
    public abstract class AzureSearchDialog<T> : IDialog<T>
    {
        private string azureSearchKey;
        private string azureSearchName;
        private string azureSearchIndex;

        private string search = string.Empty;
        private string[] facets;
        private string facet;

        public AzureSearchDialog(string azureSearchKey, string azureSearchName, string azureSearchIndex, string[] facets)
        {
            if (facets.Count() > 1)
            {
                throw new Exception("Currently either only 0 or 1 facets are supported");
            }

            this.azureSearchIndex = azureSearchIndex;
            this.azureSearchKey = azureSearchKey;
            this.azureSearchName = azureSearchName;
            this.facets = facets;
        }

        public abstract string ProcessResult(dynamic result);

        public async Task StartAsync(IDialogContext context)
        {
            var parentDialog = context.Frames[1].Target;
            bool initiatingDialog = parentDialog.GetType().Name == "LoopDialog`1";

            if (initiatingDialog)
            {
                context.Wait(InitMessageReceivedStart);
            }
            else
            {
                var replyToConversation = context.MakeMessage();
                replyToConversation.Text = "Please enter your search:";

                await context.PostAsync(replyToConversation);
                context.Wait(SearchMessageReceivedStart);
            }
        }

        public async Task InitMessageReceivedStart(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var replyToConversation = context.MakeMessage();
            replyToConversation.Text = "Please enter your search:";

            await context.PostAsync(replyToConversation);
            context.Wait(SearchMessageReceivedStart);
        }

        public async Task SearchMessageReceivedStart(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            this.search = message.Text;
            var baseUri = $"https://{this.azureSearchName}.search.windows.net/indexes/{this.azureSearchIndex}/";

            using (var client = new HttpClient() { BaseAddress = new Uri(baseUri) })
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("api-key", this.azureSearchKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var suffix = this.facets.Count() == 1 ? $"facet={HttpUtility.UrlEncode(this.facets[0])}" : string.Empty;
                var response = await client.GetAsync($"docs?api-version=2016-09-01&search={HttpUtility.UrlEncode(search)}&{suffix}");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();

                    var jobj = JObject.Parse(responseJson);

                    if (this.facets.Count() > 0)
                    {
                        foreach (var facetName in facets)
                        {
                            var facet = jobj.SelectToken($"$..['@search.facets'].{facetName}");

                            var replyToConversation = context.MakeMessage();
                            replyToConversation.Text = "Pick a facet to refine the results:";
                            replyToConversation.Recipient = message.From;
                            replyToConversation.Type = "message";
                            replyToConversation.Attachments = new List<Attachment>();
                            List<CardAction> cardButtons = new List<CardAction>();

                            foreach (var facetMember in facet)
                            {
                                var facetMemberName = facetMember.SelectToken("$..value").Value<string>(); ;
                                var facetResultCount = facetMember.SelectToken("$..count").Value<string>(); ;
                                CardAction plButton = new CardAction()
                                {
                                    Value = $"{facetMemberName}",
                                    Type = "imBack",
                                    Title = $"{facetMemberName} ({facetResultCount} results)"
                                };
                                cardButtons.Add(plButton);
                            }
                            HeroCard plCard = new HeroCard()
                            {
                                Buttons = cardButtons
                            };
                            Attachment plAttachment = plCard.ToAttachment();
                            replyToConversation.Attachments.Add(plAttachment);
                            await context.PostAsync(replyToConversation);
                        }
                        context.Wait(FacetChosenMessageReceivedStart);
                    }
                    else
                    {
                        var results = jobj.SelectToken("$.value");

                        var replyToConversation = context.MakeMessage();
                        replyToConversation.Text = "";

                        foreach (var result in results)
                        {
                            replyToConversation.Text += ProcessResult(result);
                        }

                        await context.PostAsync(replyToConversation);
                        context.Done<object>(new object());
                    }
                }
            }
        }

        public async Task FacetChosenMessageReceivedStart(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            this.facet = message.Text;
            var baseUri = $"https://{this.azureSearchName}.search.windows.net/indexes/{this.azureSearchIndex}/";

            using (var client = new HttpClient() { BaseAddress = new Uri(baseUri) })
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("api-key", this.azureSearchKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var suffix = this.facets.Count() == 1 ? $"facet={this.facets[0]}" : string.Empty;
                
                var response = await client.GetAsync($"docs?api-version=2016-09-01&search={HttpUtility.UrlEncode(this.search)}&$filter={this.facets[0]}%20eq%20'{HttpUtility.UrlEncode(this.facet)}'");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var jobj = JObject.Parse(responseJson);
                    var results = jobj.SelectToken("$.value");

                    var replyToConversation = context.MakeMessage();
                    replyToConversation.Text = "";

                    foreach (var result in results)
                    {
                        replyToConversation.Text += ProcessResult(result) + Environment.NewLine;
                    }

                    await context.PostAsync(replyToConversation);
                }
            }
            context.Done<object>(new object());
        }
    }
}