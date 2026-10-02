namespace Ogasela.Infrastructure.Accounts;

/// <summary>
/// "SocialAuth" configuration. A provider with no client ids / app id configured is treated as
/// switched off (SocialLogin.ProviderNotConfigured), so the app can ship the buttons before the
/// credentials exist. Only Facebook:AppSecret is a secret - keep it out of appsettings.json.
/// </summary>
public sealed class SocialAuthSettings
{
    public const string SectionName = "SocialAuth";

    public GoogleSettings Google { get; set; } = new();

    public AppleSettings Apple { get; set; } = new();

    public FacebookSettings Facebook { get; set; } = new();

    public sealed class GoogleSettings
    {
        /// <summary>Every OAuth client id the app signs in with (iOS, Android, web) - an ID token's audience must be one of them.</summary>
        public string[] ClientIds { get; set; } = [];
    }

    public sealed class AppleSettings
    {
        /// <summary>The iOS bundle id (native Sign in with Apple), plus a Services ID if web sign-in is added later.</summary>
        public string[] Audiences { get; set; } = [];
    }

    public sealed class FacebookSettings
    {
        public string AppId { get; set; } = string.Empty;

        public string AppSecret { get; set; } = string.Empty;
    }
}
