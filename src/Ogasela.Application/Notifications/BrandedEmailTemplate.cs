using System.Net;
using System.Text;

namespace Ogasela.Application.Notifications;

/// <summary>
/// Renders Ogasela's transactional emails into a single, consistent branded HTML shell: a top
/// accent bar, a logo lockup, a card with an eyebrow label + heading + rule + copy, and a
/// structured footer with secondary navigation and a legal line - the pattern used by premium
/// SaaS transactional email (Stripe, Linear, Vercel). Every outbound email should be built
/// through <see cref="Render"/> rather than hand-rolling HTML, so the look stays consistent as
/// more email types are added. Uses inline CSS and a table-based layout throughout, since that's
/// what actually renders reliably across email clients (Outlook desktop in particular ignores
/// most modern CSS, including gradients and box-shadow - both are used here only as a
/// progressive-enhancement touch that degrades harmlessly to a flat colour there). The optional
/// <c>prefers-color-scheme</c> block is the same kind of progressive enhancement: clients that
/// don't understand a &lt;style&gt; block simply keep the inline light-mode styles, which already
/// look correct on their own.
/// </summary>
public static class BrandedEmailTemplate
{
    private const string BrandColor = "#0B6E4F";
    private const string BrandColorDark = "#084F39";
    private const string BrandTint = "#EAF5F0";
    private const string TextColor = "#1F2933";
    private const string MutedTextColor = "#6B7280";
    private const string FaintTextColor = "#9AA5A0";
    private const string BackgroundColor = "#F3F4F1";
    private const string CardColor = "#FFFFFF";
    private const string BorderColor = "#E6E9E6";

    private const string DarkModeStyle =
        """
        @media (prefers-color-scheme: dark) {
          .og-bg { background-color: #0B120E !important; }
          .og-card { background-color: #10241B !important; border-color: #1C3B2C !important; }
          .og-heading { color: #F3F6F4 !important; }
          .og-body { color: #D6DDD9 !important; }
          .og-muted { color: #93A599 !important; }
          .og-divider { border-color: #1C3B2C !important; }
        }
        """;

    /// <param name="previewText">Hidden preheader shown next to the subject in inbox lists.</param>
    /// <param name="heading">Main heading inside the card.</param>
    /// <param name="bodyParagraphs">One or more paragraphs of body copy, rendered in order.</param>
    /// <param name="ctaText">Optional call-to-action button label.</param>
    /// <param name="ctaUrl">Optional call-to-action button link; required if <paramref name="ctaText"/> is set.</param>
    /// <param name="eyebrow">Optional short uppercase category label rendered as a pill above the heading (e.g. "SECURITY", "ACCOUNT").</param>
    public static string Render(
        string previewText,
        string heading,
        IReadOnlyList<string> bodyParagraphs,
        string? ctaText = null,
        string? ctaUrl = null,
        string? eyebrow = null)
    {
        var paragraphsHtml = new StringBuilder();
        foreach (var paragraph in bodyParagraphs)
        {
            paragraphsHtml.Append(
                $"""
                 <p class="og-body" style="margin:0 0 16px;font-size:15px;line-height:26px;color:{TextColor};">{Encode(paragraph)}</p>
                 """);
        }

        var eyebrowHtml = string.Empty;
        if (!string.IsNullOrWhiteSpace(eyebrow))
        {
            eyebrowHtml =
                $"""
                 <span style="display:inline-block;padding:5px 12px;margin:0 0 16px;border-radius:999px;background-color:{BrandTint};color:{BrandColorDark};font-size:11px;font-weight:700;letter-spacing:0.08em;">{Encode(eyebrow.ToUpperInvariant())}</span><br/>
                 """;
        }

        var ctaHtml = string.Empty;
        if (!string.IsNullOrWhiteSpace(ctaText) && !string.IsNullOrWhiteSpace(ctaUrl))
        {
            ctaHtml =
                $"""
                 <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:28px 0 4px;">
                   <tr>
                     <td style="border-radius:10px;background-color:{BrandColor};box-shadow:0 2px 4px rgba(11,110,79,0.24);">
                       <a href="{Encode(ctaUrl)}" target="_blank" rel="noopener"
                          style="display:inline-block;padding:14px 32px;font-size:15px;font-weight:700;color:#FFFFFF;text-decoration:none;border-radius:10px;letter-spacing:0.01em;">
                         {Encode(ctaText)}&nbsp;&rarr;
                       </a>
                     </td>
                   </tr>
                 </table>
                 """;
        }

        var year = DateTime.UtcNow.Year;

        return
            $"""
             <!DOCTYPE html>
             <html lang="en">
             <head>
               <meta charset="utf-8" />
               <meta name="viewport" content="width=device-width, initial-scale=1.0" />
               <meta name="color-scheme" content="light dark" />
               <meta name="supported-color-schemes" content="light dark" />
               <title>{Encode(heading)}</title>
               <style>
             {DarkModeStyle}
               </style>
             </head>
             <body class="og-bg" style="margin:0;padding:0;background-color:{BackgroundColor};font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;">
               <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{Encode(previewText)}&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;</div>

               <!-- Top accent bar - a small, consistent brand touch every Ogasela email shares. -->
               <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                 <tr>
                   <td style="height:4px;line-height:4px;font-size:0;background-color:{BrandColor};">&nbsp;</td>
                 </tr>
               </table>

               <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" class="og-bg" style="background-color:{BackgroundColor};padding:48px 16px;">
                 <tr>
                   <td align="center">
                     <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;">

                       <!-- Logo lockup: a colour-badge mark + wordmark, since there's no hosted logo image to reference. -->
                       <tr>
                         <td style="padding:0 4px 32px;">
                           <table role="presentation" cellpadding="0" cellspacing="0" border="0">
                             <tr>
                               <td style="width:40px;height:40px;background-color:{BrandColorDark};border-radius:12px;text-align:center;">
                                 <span style="display:block;width:40px;height:40px;font-size:18px;line-height:40px;font-weight:800;color:#FFFFFF;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;">O</span>
                               </td>
                               <td style="padding-left:12px;">
                                 <span class="og-heading" style="font-size:20px;font-weight:800;letter-spacing:-0.01em;color:{BrandColorDark};">Ogasela</span>
                               </td>
                             </tr>
                           </table>
                         </td>
                       </tr>

                       <!-- Message card -->
                       <tr>
                         <td class="og-card" style="background-color:{CardColor};border-radius:20px;padding:44px 40px;box-shadow:0 4px 6px -1px rgba(16,24,40,0.08),0 2px 4px -2px rgba(16,24,40,0.06);border:1px solid {BorderColor};">
                           {eyebrowHtml}
                           <h1 class="og-heading" style="margin:0 0 18px;font-size:24px;line-height:32px;font-weight:800;letter-spacing:-0.01em;color:{TextColor};">{Encode(heading)}</h1>
                           <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:0 0 22px;">
                             <tr>
                               <td style="width:40px;height:3px;line-height:3px;font-size:0;background-color:{BrandColor};border-radius:2px;">&nbsp;</td>
                             </tr>
                           </table>
                           {paragraphsHtml}
                           {ctaHtml}
                         </td>
                       </tr>

                       <!-- Footer -->
                       <tr>
                         <td style="padding:32px 4px 0;">
                           <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                             <tr>
                               <td class="og-divider" style="border-top:1px solid {BorderColor};font-size:0;line-height:0;">&nbsp;</td>
                             </tr>
                           </table>
                         </td>
                       </tr>
                       <tr>
                         <td style="padding:24px 4px 0;">
                           <p class="og-heading" style="margin:0 0 4px;font-size:13px;line-height:20px;font-weight:700;color:{TextColor};">Ogasela</p>
                           <p class="og-muted" style="margin:0 0 16px;font-size:12px;line-height:18px;color:{MutedTextColor};">Nigeria's marketplace for verified sellers.</p>
                           <p style="margin:0 0 14px;font-size:12px;line-height:18px;">
                             <a href="https://ogasela.com/help" style="color:{BrandColor};text-decoration:none;font-weight:600;">Help Center</a>
                             <span class="og-muted" style="color:{MutedTextColor};">&nbsp;&middot;&nbsp;</span>
                             <a href="https://ogasela.com/privacy" style="color:{BrandColor};text-decoration:none;font-weight:600;">Privacy</a>
                             <span class="og-muted" style="color:{MutedTextColor};">&nbsp;&middot;&nbsp;</span>
                             <a href="https://ogasela.com/terms" style="color:{BrandColor};text-decoration:none;font-weight:600;">Terms</a>
                           </p>
                           <p class="og-muted" style="margin:0 0 14px;font-size:12px;line-height:18px;color:{MutedTextColor};">
                             This email was sent because of activity on your Ogasela account.
                             If this wasn't you, contact <a href="mailto:support@ogasela.com" style="color:{BrandColor};text-decoration:none;font-weight:600;">support@ogasela.com</a>.
                           </p>
                           <p class="og-muted" style="margin:0;font-size:11px;line-height:16px;color:{FaintTextColor};">&copy; {year} Ogasela Marketplace, Lagos, Nigeria. All rights reserved.</p>
                         </td>
                       </tr>

                     </table>
                   </td>
                 </tr>
               </table>
             </body>
             </html>
             """;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
