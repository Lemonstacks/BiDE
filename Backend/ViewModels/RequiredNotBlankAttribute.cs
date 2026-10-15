using System.ComponentModel.DataAnnotations;
namespace BiDE.ViewModels
{
    // Like [Required], but a whitespace-only string also counts as missing. This keeps the
    // old IsNullOrWhiteSpace behavior, which the stock [Required] does not provide.
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class RequiredNotBlankAttribute : RequiredAttribute
    {
        public override bool IsValid(object? value) =>
            value is string s ? !string.IsNullOrWhiteSpace(s) : value != null;
    }
}
