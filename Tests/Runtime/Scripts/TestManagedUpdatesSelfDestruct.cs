using System;
using UnityEngine;
using UnityEngine.Events;

namespace Miscreant.Lifecycle.RuntimeTests
{
	public sealed class TestManagedUpdatesSelfDestruct : CustomUpdateBehaviour
	{
		public UnityEvent OnSelfDestruct = new UnityEvent();
		public const float DEFAULT_COUNTDOWN_DURATION = 0.5f;

		float _expirationTime = -1;

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

		[ContextMenu("Destroy after half second")]
		public void DestroyAfterHalfSec()
		{
			StartCountdown(0.5f);
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
