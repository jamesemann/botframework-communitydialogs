using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityDialogs
{

    [Serializable]
    public abstract class FacebookAccountLinkingDialog : IDialog<object>
    {
        protected string Message { get; set; }

        protected string LoginUrl { get; set; }

        public FacebookAccountLinkingDialog(string message, string loginUrl)
        {
            this.Message = message;
            this.LoginUrl = loginUrl;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedStart);
        }

        public async Task MessageReceivedStart(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var reply = context.MakeMessage();
            var attachment = CreateLoginLogoutPayload(Message,LoginUrl);
            reply.ChannelData = JObject.FromObject(new { attachment });

            await context.PostAsync(reply);

            // State transition - wait for 'account link/unlink' message
            context.Wait(LinkedOrUninlinked);
        }

        public abstract Task Linked(IDialogContext context, IMessageActivity argument);

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
                        await Unlinked(context, activity);
                    }
                    else
                    {
                        await Linked(context, activity);
                    }

                    context.Done<object>(new object());
                }
                else
                {
                    var reply = context.MakeMessage();
                    var attachment = CreateLoginLogoutPayload(Message,LoginUrl);
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
                        },
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
