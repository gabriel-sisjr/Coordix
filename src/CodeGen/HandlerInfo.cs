using System;
using Microsoft.CodeAnalysis;

namespace Coordix.CodeGen;

/// <summary>
/// Represents information about a discovered handler for code generation.
/// </summary>
internal readonly struct HandlerInfo : IEquatable<HandlerInfo>
{
	public HandlerInfo(
		string handlerTypeName,
		string handlerNamespace,
		HandlerKind kind,
		string requestOrNotificationType,
		string? responseType = null)
	{
		HandlerTypeName = handlerTypeName;
		HandlerNamespace = handlerNamespace;
		Kind = kind;
		RequestOrNotificationType = requestOrNotificationType;
		ResponseType = responseType;
	}

	/// <summary>
	/// The simple name of the handler class (e.g., "MyRequestHandler").
	/// </summary>
	public string HandlerTypeName { get; }

	/// <summary>
	/// The namespace of the handler class (e.g., "MyApp.Handlers").
	/// </summary>
	public string HandlerNamespace { get; }

	/// <summary>
	/// Fully qualified handler type name (namespace + type name).
	/// </summary>
	public string FullHandlerTypeName => $"{HandlerNamespace}.{HandlerTypeName}";

	/// <summary>
	/// The kind of handler (request with response, request without response, or notification).
	/// </summary>
	public HandlerKind Kind { get; }

	/// <summary>
	/// Fully qualified name of the request or notification type.
	/// </summary>
	public string RequestOrNotificationType { get; }

	/// <summary>
	/// Fully qualified name of the response type (null for requests without response and notifications).
	/// </summary>
	public string? ResponseType { get; }

	public bool Equals(HandlerInfo other)
	{
		return HandlerTypeName == other.HandlerTypeName &&
				 HandlerNamespace == other.HandlerNamespace &&
				 Kind == other.Kind &&
				 RequestOrNotificationType == other.RequestOrNotificationType &&
				 ResponseType == other.ResponseType;
	}

	public override bool Equals(object? obj)
	{
		return obj is HandlerInfo other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = HandlerTypeName.GetHashCode();
			hashCode = (hashCode * 397) ^ HandlerNamespace.GetHashCode();
			hashCode = (hashCode * 397) ^ (int)Kind;
			hashCode = (hashCode * 397) ^ RequestOrNotificationType.GetHashCode();
			hashCode = (hashCode * 397) ^ (ResponseType?.GetHashCode() ?? 0);
			return hashCode;
		}
	}

	public static bool operator ==(HandlerInfo left, HandlerInfo right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(HandlerInfo left, HandlerInfo right)
	{
		return !left.Equals(right);
	}
}

/// <summary>
/// Specifies the kind of handler discovered.
/// </summary>
internal enum HandlerKind
{
	/// <summary>
	/// IRequestHandler&lt;TRequest, TResponse&gt;
	/// </summary>
	RequestWithResponse,

	/// <summary>
	/// IRequestHandler&lt;TRequest&gt;
	/// </summary>
	RequestWithoutResponse,

	/// <summary>
	/// INotificationHandler&lt;TNotification&gt;
	/// </summary>
	Notification
}
