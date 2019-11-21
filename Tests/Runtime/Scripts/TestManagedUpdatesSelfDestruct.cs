using System;
using UnityEngine;
using UnityEngine.Events;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	public sealed class TestManagedUpdatesSelfDestruct : CustomUpdateBehaviour
	{
		public static UnityEvent OnSelfDestruct;
		public const float DEFAULT_COUNTDOWN_DURATION = 0.5f;

		float _expirationTime = -1;

		private void Awake()
		{
			StartCountdown(DEFAULT_COUNTDOWN_DURATION);
		}

		public override void ManagedUpdate()
		{
			CheckExpiration(Time.time);
		}

		public override void ManagedFixedUpdate()
		{
			CheckExpiration(Time.fixedTime);
		}

		public void CheckExpiration(float currentTime)
		{
			if (_expirationTime >= 0 && currentTime >= _expirationTime)
			{
				Destroy(gameObject);

				// Destroy before invoking the event, so assertions can be checked after the component is removed from its list.
				OnSelfDestruct?.Invoke();
			}
		}

		public void StartCountdown(float remainingTime)
		{
			if (remainingTime < 0)
			{
				throw new ArgumentException("Countdown time must be >= 0. Time received: " + remainingTime);
			}

			this._expirationTime = Time.time + remainingTime;
		}
	}
}
