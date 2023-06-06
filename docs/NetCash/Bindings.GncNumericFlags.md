# Bindings.GncNumericFlags enumeration

```csharp
public enum GncNumericFlags
```

## Values

| name | value | description |
| --- | --- | --- |
| GNC_HOW_RND_FLOOR | `1` | Round toward -infinity |
| GNC_HOW_RND_CEIL | `2` | Round toward +infinity |
| GNC_HOW_RND_TRUNC | `3` | Truncate fractions (round toward zero) |
| GNC_HOW_RND_PROMOTE | `4` | Promote fractions (round away from zero) |
| GNC_HOW_RND_ROUND_HALF_DOWN | `5` | Round to the nearest integer, rounding toward zero when there are two equidistant nearest integers. |
| GNC_HOW_RND_ROUND_HALF_UP | `6` | Round to the nearest integer, rounding away from zero when there are two equidistant nearest integers. |
| GNC_HOW_RND_ROUND | `7` | Use unbiased ("banker's") rounding. This rounds to the nearest integer, and to the nearest even integer when there are two equidistant nearest integers. This is generally the one you should use for financial quantities. |
| GNC_HOW_RND_NEVER | `8` | Never round at all, and signal an error if there is a fractional result in a computation. |
| GNC_HOW_DENOM_EXACT | `16` | Use any denominator which gives an exactly correct ratio of numerator to denominator. Use EXACT when you do not wish to lose any information in the result but also do not want to spend any time finding the "best" denominator. |
| GNC_HOW_DENOM_REDUCE | `32` | Reduce the result value by common factor elimination, using the smallest possible value for the denominator that keeps the correct ratio. The numerator and denominator of the result are relatively prime. |
| GNC_HOW_DENOM_LCD | `48` | Find the least common multiple of the arguments' denominators and use that as the denominator of the result. |
| GNC_HOW_DENOM_FIXED | `64` | All arguments are required to have the same denominator, that denominator is to be used in the output, and an error is to be signaled if any argument has a different denominator. |
| GNC_HOW_DENOM_SIGFIG | `80` | Round to the number of significant figures given in the rounding instructions by the GNC_HOW_DENOM_SIGFIGS () macro. |

## See Also

* class [Bindings](./Bindings.md)
* namespace [NetCash](../netcash.md)

<!-- DO NOT EDIT: generated by xmldocmd for netcash.dll -->