using System;
using UnityEngine;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	public sealed class TestManagedUpdatesSceneController : MonoBehaviour
	{
		public Action OnMonoBehaviourUpdate;
		public Action OnMonoBehaviourFixedUpdate;

		private void Update()
		{
			OnMonoBehaviourUpdate?.Invoke();
		}

		private void FixedUpdate()
		{
			OnMonoBehaviourFixedUpdate?.Invoke();
		}
	}
}