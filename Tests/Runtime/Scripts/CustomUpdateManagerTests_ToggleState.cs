using NUnit.Framework;
using System.Linq;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public sealed class CustomUpdateManagerTests_ToggleState
	{
		private FakeEnvironment _environment;

		[SetUp]
		public void SetUp()
		{
			_environment = new FakeEnvironment(CustomUpdateManagerTests.DEFAULT_GROUP_NAME);
		}

		[TearDown]
		public void TearDown()
		{
			_environment.Dispose();
			_environment = null;
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.inactiveTogglePermutationsNeedGameObject))]
		public void TryAdd_SingleGameObjectToggledOn_CorrectFlagsAddedToSystem(ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.GameObjectActive);
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.inactiveTogglePermutationsNeedComponent))]
		public void TryAdd_SingleComponentToggledOn_CorrectFlagsAddedToSystem(ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.ComponentEnabled);
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.inactiveTogglePermutationsNeedUpdate))]
		public void TryAdd_SingleUpdateFlagToggledOn_CorrectFlagsAddedToSystem(ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.Update);
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.inactiveTogglePermutationsNeedFixedUpdate))]
		public void TryAdd_SingleFixedUpdateFlagToggledOn_CorrectFlagsAddedToSystem(ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.FixedUpdate);
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.allActiveTogglePermutations))]
		public void TryRemove_SingleGameObjectToggledOff_RemovedFromSystem(ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig & ~ObjectToggleConfig.GameObjectActive);
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.allActiveTogglePermutations))]
		public void TryRemove_SingleComponentToggledOff_RemovedFromSystem(ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig & ~ObjectToggleConfig.ComponentEnabled);
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.activeUpdateTogglePermutations))]
		public void TryRemove_SingleUpdateToggledOff_RemovedFromSystem(ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig & ~ObjectToggleConfig.Update);
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.activeFixedUpateTogglePermutations))]
		public void TryRemove_SingleFixedUpdateToggledOff_RemovedFromSystem(ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig & ~ObjectToggleConfig.FixedUpdate);
		}

		private static void RunToggleTest(FakeEnvironment env, ObjectToggleConfig initialConfig, ObjectToggleConfig finalConfig)
		{
			// Arrange
			TestBasicManagedUpdatesComponent[] components;
			env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out components,
				CustomUpdateManagerTests.DEFAULT_GROUP_NAME,
				initialConfig
			);

			FakeEnvironment.GetExpectedUpdateFlagsFromConfig(
				finalConfig,
				out bool updateExpectedFinal,
				out bool fixedUpdateExpectedFinal
			);

			// Act
			CustomUpdateBehaviour component = components[0];
			env.SetToggleConfig(component, finalConfig);

			// Assert
			env.manager.CheckSystemForComponent(component, out bool updateFound, out bool fixedUpdateFound);
			Assert.That(
				(updateExpectedFinal == updateFound) && (fixedUpdateExpectedFinal == fixedUpdateFound),
				$"Configuration's expected state was not reflected in the manager.\n" +
					$"Update Expected/Result: {updateExpectedFinal}/{updateFound}\n" +
					$"FixedUpdate Expected/Result: {fixedUpdateExpectedFinal}/{fixedUpdateFound}"
			);
		}

	}
}