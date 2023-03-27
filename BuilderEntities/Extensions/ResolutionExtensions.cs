using BuildEntities;

namespace BuilderEntities.Extensions;

public static class ResolutionExtensions
{
    public static string GetBlobPrefixByResolution(this Resolution resolution)
    {
        switch (resolution)
        {
            case Resolution.Hd:
                return "hd";
            case Resolution.FourK:
                return "4k";
            default:
                return "free";
        }
    }
}
