namespace Src.Websites
{
    public class SciFier
    {
        private List<string> SciFierLinks = new List<string>();
        private List<EntryModel> SciFierData = new();
        public const string WEBSITE_TITLE = "SciFier";
        private static readonly Logger LOGGER = LogManager.GetLogger("SciFierLogs");
        private const Region WEBSITE_REGION = Region.America | Region.Europe | Region.Britain | Region.Canada;
    }
}