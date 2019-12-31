using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Miscreant.Lifecycle
{
	internal sealed class IntrusiveUpdateList : IntrusiveList
	{
		protected override void ExecuteCurrent()
		{
			current.ManagedUpdate();
		}

		protected override void Advance()
		{
			current = current.nextUpdate;
		}

		internal override void AddToTail(CustomUpdateBehaviour component)
		{
			// TODO: Miscreant: Only need to check once of these references. Since the lists are circular, if one of these refs is null, both must be null. 
			if (!ReferenceEquals(component.nextUpdate, null) || !ReferenceEquals(component.previousUpdate, null))
			{
				return;
			}
			count++;

			if (ReferenceEquals(head, null))
			{
				head = component;
				head.previousUpdate = head;
				head.nextUpdate = head;
				return;
			}

			Add(component, head.previousUpdate, head);
		}

		private void Add(CustomUpdateBehaviour node, CustomUpdateBehaviour prev, CustomUpdateBehaviour next)
		{
			node.nextUpdate = next;
			node.previousUpdate = prev;

			next.previousUpdate = node;
			prev.nextUpdate = node;
		}

		internal override void Remove(CustomUpdateBehaviour component)
		{
			// Only need to check one of the link references. Since the lists are circular, if one of these refs is null, both must be null. 
			if (ReferenceEquals(component.nextUpdate, null))
			{
				return;
			}
			count--;

			if (count == 0)
			{
				// TODO: Miscreant: Elegantly handle "current" field  when removing the last element in the list. Need to work out semantics there, too. 
				head.previousUpdate = null;
				head.nextUpdate = null;
				head = null;
				return;
			}

			if (ReferenceEquals(component, head))
			{
				head = head.nextUpdate;
			}

			component.nextUpdate.previousUpdate = component.previousUpdate;
			component.previousUpdate.nextUpdate = component.nextUpdate;

			// This specifically handles the case where a component destroys itself from any type of managed update callback. By resetting 'current' to the 
			// previous link, 'current' will not be null when 'Advance()' is called, and we'll process the expected next link during the next iteration. 
			// Since it was the 'current' update callback that brought us here, there's no danger of accidentally processing 'current' a second time.
			if (ReferenceEquals(current, component))
			{
				current = component.previousUpdate;
			}

			component.previousUpdate = null;
			component.nextUpdate = null;
		}
	}
}
