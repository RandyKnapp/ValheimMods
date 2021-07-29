using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using ThunderKit.Core.Data;
using UnityEngine;

namespace ThunderKit.Integrations.Thunderstore
{
    /// <summary>
    /// ThunderstoreAPI provides an interface to the Thunderstore API
    /// Currently supports Listing, Downloading, and Searching for packages.
    /// </summary>
    public class ThunderstoreAPI
    {
        class GZipWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                return request;
            }
        }
        internal class ThunderstoreLoadBehaviour : MonoBehaviour { }

        private static PackageListing[] packageListings;

        public static IEnumerable<PackageListing> PackageListings => packageListings.ToList();

        static string PackageListApi => ThunderKitSetting.GetOrCreateSettings<ThunderstoreSettings>().ThunderstoreUrl + "/api/v1/package/";
        public static event EventHandler PagesLoaded;

        public static void ReloadPages()
        {
            Debug.Log($"Updating Package listing: {PackageListApi}");
            using (var client = new GZipWebClient())
            {
                client.DownloadStringCompleted += Client_DownloadStringCompleted;
                var address = new Uri(PackageListApi);
                client.DownloadStringAsync(address);
            }
        }

        private static void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var json = $"{{ \"{nameof(PackagesResponse.results)}\": {e.Result} }}";
            var response = JsonUtility.FromJson<PackagesResponse>(json);
            packageListings = response.results;
            PagesLoaded?.Invoke(null, EventArgs.Empty);
            Debug.Log($"Package listing updated: {PackageListApi}");
        }

        public static IEnumerable<PackageListing> LookupPackage(string name)
        {
            if (packageListings.Length == 0)
                return Enumerable.Empty<PackageListing>();
            else
                return packageListings.Where(package => IsMatch(package, name)).ToArray();
        }

        static bool IsMatch(PackageListing package, string name)
        {
            CompareInfo comparer = CultureInfo.CurrentCulture.CompareInfo;
            var compareOptions = CompareOptions.IgnoreCase;
            var nameMatch = comparer.IndexOf(package.name, name, compareOptions) >= 0;
            var fullNameMatch = comparer.IndexOf(package.full_name, name, compareOptions) >= 0;

            var latest = package.versions.OrderByDescending(pck => pck.version_number).First();
            var latestFullNameMatch = comparer.IndexOf(latest.full_name, name, compareOptions) >= 0;
            return nameMatch || fullNameMatch || latestFullNameMatch;
        }

    }
}