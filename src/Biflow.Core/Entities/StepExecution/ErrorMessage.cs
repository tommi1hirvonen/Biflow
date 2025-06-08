﻿using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

[ComplexType]
public record ErrorMessage(string Message, string? Exception);