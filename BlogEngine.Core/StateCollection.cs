namespace BlogEngine.Core
{
  /// <summary>
  /// A generic collection with the ability to 
  /// check if it has been changed.
  /// </summary>
  [System.Serializable]
  public class StateList<T> : System.Collections.Generic.List<T>
  {

    #region Base overrides

		///// <summary>
		///// Inserts an element into the collection at the specified index and marks it changed.
		///// </summary>
		//protected override void InsertItem(int index, T item)
		//{
		//  base.InsertItem(index, item);
		//  _IsChanged = true;
		//}

		///// <summary>
		///// Removes all the items in the collection and marks it changed.
		///// </summary>
		//protected override void ClearItems()
		//{
		//  base.ClearItems();
		//  _IsChanged = true;
		//}

		///// <summary>
		///// Removes the element at the specified index and marks the collection changed.
		///// </summary>
		//protected override void RemoveItem(int index)
		//{
		//  base.RemoveItem(index);
		//  _IsChanged = true;
		//}

		///// <summary>
		///// Replaces the element at the specified index and marks the collection changed.
		///// </summary>
		//protected override void SetItem(int index, T item)
		//{
		//  base.SetItem(index, item);
		//  _IsChanged = true;
		//}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			string hash = string.Empty;
			foreach (T item in this)
			{
				hash += item.GetHashCode().ToString();
			}

			return hash.GetHashCode();
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj.GetType() == this.GetType())
			{
				return obj.GetHashCode() == this.GetHashCode();
			}

			return false;
		}

    #endregion

		private int _HasCode = 0;

    /// <summary>
    /// Gets if this object's data has been changed.
    /// </summary>
    /// <returns>A value indicating if this object's data has been changed.</returns>
    public virtual bool IsChanged
    {
      get 
			{
				return this.GetHashCode() != _HasCode;
			}
    }

		/// <summary>
		/// Marks the object as being clean, 
		/// which means not changed.
		/// </summary>
		public virtual void MarkOld()
		{
			_HasCode = this.GetHashCode();
			base.TrimExcess();
		}

  }
}