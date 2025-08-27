using CheapLoc;

namespace TeraSyncV2.Localization;

public static class Strings
{
    public static ToSStrings ToS { get; set; } = new();

    public class ToSStrings
    {
        public readonly string AgreeLabel = Loc.Localize("AgreeLabel", "I agree");
        public readonly string AgreementLabel = Loc.Localize("AgreementLabel", "Terms of Service");
        public readonly string ButtonWillBeAvailableIn = Loc.Localize("ButtonWillBeAvailableIn", "'I agree' button will be available in");
        public readonly string LanguageLabel = Loc.Localize("LanguageLabel", "Language");

        public readonly string Paragraph1 = Loc.Localize("Paragraph1",
            "Hey! Just so you know, when you use TeraSync, your active mods and character appearance get uploaded to the server automatically. " +
            "Don't worry though - I only upload the files that are actually being used, not your entire mod collection.");

        public readonly string Paragraph2 = Loc.Localize("Paragraph2",
            "Quick heads up about data usage - if you're on a limited internet plan, syncing mods might eat into your data cap. " +
            "I compress everything to save bandwidth, but depending on your connection speed, changes might take a moment to show up. " +
            "The good news? Files you've already uploaded won't be uploaded again.");

        public readonly string Paragraph3 = Loc.Localize("Paragraph3",
            "Your mods stay private - they're only shared with people who are syncing with you directly. " +
            "Be mindful about who you sync with, because they'll download and cache your active mods locally. " +
            "To protect mod creators, cached files get randomized names so they're harder to redistribute.");

        public readonly string Paragraph4 = Loc.Localize("Paragraph4",
            "I've done my best to keep everything secure, but let's be real - nothing on the internet is 100% safe. Use common sense and don't sync with random people you don't trust.");

        public readonly string Paragraph5 = Loc.Localize("Paragraph5",
            "Your uploaded files stick around on the server as long as someone's using them. " +
            "Unused files get automatically cleaned up after a while to save space. " +
            "Want to delete everything you've uploaded? You can wipe your data anytime. " +
            "Oh, and the server doesn't track which files belong to which mods - it's all anonymous.");

        public readonly string Paragraph6 = Loc.Localize("Paragraph6",
            "That's about it! This service is provided as-is. If you run into issues or see someone abusing the system, hit me up on GitHub.");

        public readonly string ReadLabel = Loc.Localize("ReadLabel", "READ THIS CAREFULLY");
    }
}