// Backend purpose: Defines an application-level exception used for controlled failures.
using System;

namespace BookingSystem.Application.Exceptions
{
    public class AppException : Exception
    {
        public AppException(string message) : base(message) { }
    }
}