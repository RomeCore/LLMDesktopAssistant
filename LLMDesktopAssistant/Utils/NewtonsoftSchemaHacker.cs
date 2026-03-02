using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Schema;

namespace LLMDesktopAssistant.Utils
{
	/// <summary>
	/// Hehehehehehehehehehehehehehehehehehehehehehehehehehehehehehehehe
	/// </summary>
	public static class NewtonsoftSchemaHacker
	{
		/// <summary>
		/// Hacks the Newtonsoft.Json schema to make it more friendly for our purposes.
		/// </summary>
		public static void Hack()
		{
			var assembly = typeof(Newtonsoft.Json.Schema.JSchema).Assembly;
			var licenseHelpersType = assembly.GetType("Newtonsoft.Json.Schema.Infrastructure.Licensing.LicenseHelpers")!;
			var registegedLicenseField = licenseHelpersType.GetField("_registeredLicense", BindingFlags.NonPublic | BindingFlags.Static)!;
			var licenseDetailsType = assembly.GetType("Newtonsoft.Json.Schema.Infrastructure.Licensing.LicenseDetails")!;
			
			var licenseDetails = Activator.CreateInstance(licenseDetailsType);
			licenseDetailsType.GetProperty("Id")!.SetValue(licenseDetails, 1);
			licenseDetailsType.GetProperty("ExpiryDate")!.SetValue(licenseDetails, DateTime.MaxValue);
			licenseDetailsType.GetProperty("Type")!.SetValue(licenseDetails, "Developer");

			registegedLicenseField.SetValue(null, licenseDetails);
		}
	}
}