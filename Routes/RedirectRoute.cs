using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;

namespace TwitchStreamsVkNotifications.Routes;

/// <summary>
/// Обработка подключений к /redirect
/// </summary>
public class RedirectRoute
{
    public static IResult GetAsync(IOptions<MyOptions> options)
    {
        return TypedResults.Content(
            $$"""
            <!DOCTYPE html>
            <html>

            <head>
                <title>я юра</title>
                <script>
                    var hash = window.location.hash.substring(1);

                    fetch("{{new Uri(options.Value.ServerUrl, "setup")}}",
                        {
                            method: "POST",
                            body: hash
                        })
                        .then(response => {
                            window.location.href = "{{options.Value.ServerUrl}}";
                        })
                        .catch(reason => {
                            console.log(reason);
                        });
                </script>
            </head>

            <body>
                ща-ща...
            </body>

            </html>
            """, contentType: "text/html", Encoding.UTF8);
    }
}
