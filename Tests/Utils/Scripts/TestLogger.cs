#if UNITY_EDITOR

using UnityEngine;
using System.IO;
using System.Text;
using UnityEditor.TestTools.TestRunner.Api;

namespace Miscreant.Lifecycle.TestUtils
{
	/// <summary>
	/// Editor-only class. Logs a list of all tests each time they are run in the editor. 
	/// </summary>
	public static class TestLogger
	{
		public static void SaveToDisk(ITestAdaptor rootNode, string directoryPath, string fileName)
		{
			if (!Directory.Exists(directoryPath))
			{
				throw new DirectoryNotFoundException("Directory not found at the specified path: " + directoryPath);
			}

			string logText = GenerateLogStringForTestNode(rootNode);

			string filePath = directoryPath + "/" + fileName;
			File.WriteAllText(filePath, logText);

			Debug.Log("Saved test log: " + filePath);
		}

		private static string GenerateLogStringForTestNode(ITestAdaptor testNode)
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