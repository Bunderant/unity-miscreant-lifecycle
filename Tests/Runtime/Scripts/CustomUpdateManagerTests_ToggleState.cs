using NUnit.Framework;
using System.Linq;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public sealed class CustomUpdateManagerTests_ToggleState
	{
		private static ObjectToggleConfig[] _allActiveToggleStates = CustomUpdateManagerTests.allActiveTogglePermutations;

		private static ObjectToggleConfig[] _activeUpdateToggleStates = _allActiveToggleStates.Where(
			x => { return x.HasFlag(ObjectToggleConfig.Update); }).Distinct().ToArray();

		private static ObjectToggleConfig[] _activeFixedUpateToggleStates = _allActiveToggleStates.Where(
			x => { return x.HasFlag(ObjectToggleConfig.FixedUpdate); }).Distinct().ToArray();

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedGameObject = _allActiveToggleStates.Select(
			x => { return x & ~ObjectToggleConfig.GameObjectActive; }).Distinct().ToArray();

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedComponent = _allActiveToggleStates.Select(
			x => { return x & ~ObjectToggleConfig.ComponentEnabled; }).Distinct().ToArray();

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedUpdate = _allActiveToggleStates.Select(
			x => { return x & ~ObjectToggleConfig.Update; }).Distinct().ToArray();

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedFixedUpdate = _allActiveToggleStates.Select(
			x => { return x & ~ObjectToggleConfig.FixedUpdate; }).Distinct().ToArray();

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

		[Test, Sequential]
		public void TryAdd_SingleGameObjectToggledOn_CorrectFlagsAddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedGameObject))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.GameObjectActive);
		}

		[Test, Sequential]
		public void TryAdd_SingleComponentToggledOn_CorrectFlagsAddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedComponent))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.ComponentEnabled);
		}

		[Test, Sequential]
		public void TryAdd_SingleUpdateFlagToggledOn_CorrectFlagsAddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedUpdate))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.Update);
		}

		[Test, Sequential]
		public void TryAdd_SingleFixedUpdateFlagToggledOn_CorrectFlagsAddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedFixedUpdate))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.FixedUpdate);
		}

		[Test, Sequential]
		public void TryRemove_SingleGameObjectToggledOff_RemovedFromSystem([ValueSource(nameof(_allActiveToggleStates))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig & ~ObjectToggleConfig.GameObjectActive);
		}

		[Test, Sequential]
		public void TryRemove_SingleComponentToggledOff_RemovedFromSystem([ValueSource(nameof(_allActiveToggleStates))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig & ~ObjectToggleConfig.ComponentEnabled);
		}

		[Test, Sequential]
		public void TryRemove_SingleUpdateToggledOff_RemovedFromSystem([ValueSource(nameof(_activeUpdateToggleStates))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig & ~ObjectToggleConfig.Update);
		}

		[Test, Sequential]
		public void TryRemove_SingleFixedUpdateToggledOff_RemovedFromSystem([ValueSource(nameof(_activeFixedUpateToggleStates))] ObjectToggleConfig initialConfig)
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