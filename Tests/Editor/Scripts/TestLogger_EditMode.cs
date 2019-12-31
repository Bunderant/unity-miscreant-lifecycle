#if UNITY_EDITOR

using UnityEngine;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner.Api;

namespace Miscreant.Lifecycle.EditorTests
{
	using TestUtils;

	/// <summary>
	/// Editor-only class. Logs a list of all EDIT mode tests each time any edit mode test is run in the editor. 
	/// </summary>
	[SetUpFixture]
	public sealed class TestLogger_EditMode
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			var api = ScriptableObject.CreateInstance<TestRunnerApi>();	
			api.RetrieveTestList(TestMode.EditMode, (testRoot) =>
			{
				TestLogger.SaveToDisk(
					testRoot,
					Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + "Packages/Lifecycle/Tests",
					"TestLog-EditMode.txt"
				);
			});
		}
	}
}

#endif