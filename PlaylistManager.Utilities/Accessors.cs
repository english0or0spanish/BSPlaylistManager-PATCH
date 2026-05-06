using HMUI;
using IPA.Utilities;

namespace PlaylistManager.Utilities
{
    internal static class Accessors
    {
        public static readonly FieldAccessor<ScrollView, IVRPlatformHelper>.Accessor PlatformHelperAccessor =
            FieldAccessor<ScrollView, IVRPlatformHelper>.GetAccessor("_platformHelper");

        public static readonly FieldAccessor<HoverHint, HoverHintController>.Accessor HoverHintControllerAccessor =
            FieldAccessor<HoverHint, HoverHintController>.GetAccessor("_hoverHintController");
    }
}