#if UNITY_EDITOR

using UnityEngine;
using NUnit.Framework;
using System.IO;
using System.Text;
using UnityEditor.TestTools.TestRunner.Api;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	[SetUpFixture]
	/// <summary>
	/// Editor-only class. Logs a list of all tests each time they are run in the editor. 
	/// </summary>
	public sealed class TestLogger
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			var api = ScriptableObject.CreateInstance<TestRunnerApi>();	
			api.RetrieveTestList(TestMode.PlayMode, (testRoot) =>
			{
				SaveToDisk(
					GenerateLogStringForTestNode(testRoot),
					Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + "Packages/Lifecycle/Tests",
					"TestLog-PlayMode.txt"
				);
			});
		}

		private void SaveToDisk(string logText, string directoryPath, string fileName)
		{
			if (!Directory.Exists(directoryPath))
			{
				throw new DirectoryNotFoundException("Directory not found at the specified path: " + directoryPath);
			}

			string filePath = directoryPath + "/" + fileName;
			File.WriteAllText(filePath, logText);

			Debug.Log("Saved test log: " + filePath);
		}

		private string GenerateLogStringForTestNode(ITestAdaptor testNode)
		{
			var logText = new StringBuilder();

			LogChildrenRecursively(testNode, 0);

			void LogChildrenRecursively(ITestAdaptor child, int tabIndentLevel)
			{
				for (int i = 0; i < tabIndentLevel; i++)
				{
					logText.Append('\t');
				}

				logText.AppendLine(child.Name);

				if (child.HasChildren)
				{
					foreach (var newChild in child.Children)
					{
						LogChildrenRecursively(newChild, tabIndentLevel + 1);
					}
				}
			}

			return logText.ToString();
		}
	}
}

#endif