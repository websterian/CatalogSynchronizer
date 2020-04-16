using Sitecore.Commerce.Core;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Policies
{
    public class SynchronizeCatalogPolicy : Policy
    {
        public SynchronizeCatalogPolicy()
        {
            SourceFolderLocation = string.Empty;
            SuccessFolderLocation = string.Empty;
            FailFolderLocation = string.Empty;
            SkipRelationships = false;
            ExcludeLogInResults = false;
        }

        public string SourceFolderLocation { get; set; }
        public string SuccessFolderLocation { get; set; }
        public string FailFolderLocation { get; set; }
        public bool SkipRelationships { get; set; }
        public bool ExcludeLogInResults { get; set; }
    }
}
