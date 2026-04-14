using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Server.CommanderApi.Configuration;

/// <summary>
///     Custom route constraint for unsigned 32-bit integers.
///     ASP.NET Core only ships with 'int' (signed 32-bit) constraint built-in.
///     Register in DI: services.AddRouting(o => o.ConstraintMap.Add("uint", typeof(UIntRouteConstraint)));
/// </summary>
public class UIntRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (values.TryGetValue(routeKey, out var value) && value != null)
        {
            if (value is uint)
            {
                return true;
            }

            var stringValue = Convert.ToString(value);
            if (stringValue != null && uint.TryParse(stringValue, out _))
            {
                return true;
            }
        }

        return false;
    }
}
