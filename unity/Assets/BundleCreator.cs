
using UnityEditor;
using System.IO;

public class BundleCreator {

	[MenuItem("MSCMP/Build Asset Bundles")]
	static void BuildABs() {
		BuildPipeline.BuildAssetBundles("../data",
				BuildAssetBundleOptions.UncompressedAssetBundle,
				BuildTarget.StandaloneWindows);
	}
}
