#if UNITY_EDITOR

using UnityEngine;
using NUnit.Framework;
using UnityEditor.TestTools.TestRunner.Api;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	using TestUtils;

	/// <summary>
	/// Editor-only class. Logs a list of all PLAY mode tests each time any play mode test is run in the editor. 
	/// </summary>
	[SetUpFixture]
	public sealed class TestLogger_PlayMode
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			var api = ScriptableObject.CreateInstance<TestRunnerApi>();	
			api.RetrieveTestList(TestMode.PlayMode, (testRoot) =>
			{
				TestLogger.SaveToDisk(
					testRoot,
					Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + "Packages/Lifecycle/Tests",
					"TestLog-PlayMode.txt"
				);
			});
		}
	}
}

#endif