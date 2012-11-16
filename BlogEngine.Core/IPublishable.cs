#region Using

using System;
using System.Collections.Generic;

#endregion

namespace BlogEngine.Core
{
  /// <summary>
  /// An interface implemented by the classed that can be published.
  /// <remarks>
  /// To implemnet this interface means that the class can be searched
  /// from the search page and that it can be syndicated in RSS and ATOM.
  /// </remarks>
  /// </summary>
  public interface IPublishable
  {
    /// <summary>
    /// Gets the title of the object
    /// </summary>
    String Title { get; }

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <value>The content.</value>
    String Content { get;}

    /// <summary>
    /// Gets the date created.
    /// </summary>
    /// <value>The date created.</value>
    DateTime DateCreated { get; }

    /// <summary>
    /// Gets the date modified.
    /// </summary>
    /// <value>The date modified.</value>
    DateTime DateModified { get; }

    /// <summary>
    /// Gets the id.
    /// </summary>
    /// <value>The id.</value>
    Guid Id { get;  }

    /// <summary>
    /// Gets the relative link.
    /// </summary>
    /// <value>The relative link.</value>
    String RelativeLink { get;}

	/// <summary>
	/// Gets the absolute link.
	/// </summary>
	/// <value>The absolute link.</value>
	Uri AbsoluteLink { get;}

    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <value>The description.</value>
    String Description { get;}

    /// <summary>
    /// Gets the author.
    /// </summary>
    /// <value>The author.</value>
    String Author { get;}

    /// <summary>
    /// Raises the <see cref="E:Serving"/> event.
    /// </summary>
    /// <param name="eventArgs">The <see cref="BlogEngine.Core.ServingEventArgs"/> instance containing the event data.</param>
    void OnServing(ServingEventArgs eventArgs);

    /// <summary>
    /// Gets the categories.
    /// </summary>
    /// <value>The categories.</value>
    StateList<Category> Categories { get;}

    /// <summary>
    /// Gets whether or not this item is published.
    /// </summary>
    bool IsPublished { get; }

    /// <summary>
    /// Gets whether or not this item should be shown.
    /// </summary>
    bool IsVisible { get;}

    /// <summary>
    /// Gets whether or not this item is visible to the public.
    /// </summary>
    bool IsVisibleToPublic { get; }
  }
}