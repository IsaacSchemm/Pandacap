﻿using Pandacap.Data;

namespace Pandacap.Models
{
    public record ActivityInfo(
        RemoteActivity RemoteActivity,
        IUserDeviation? Post);
}