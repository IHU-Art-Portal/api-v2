﻿using System.Security.Claims;
using Theatrical.Data.Models;
using Theatrical.Dto.LoginDtos;
using Theatrical.Dto.ResponseWrapperFolder;
using Theatrical.Services.Jwt;
using Theatrical.Services.Repositories;

namespace Theatrical.Services.Validation;

public interface IUserValidationService
{
    Task<ValidationReport> ValidateForRegister(RegisterUserDto userdto);
    Task<(ValidationReport report, User user)> ValidateForLogin(LoginUserDto loginUserDto);
    ValidationReport ValidateUser(string? jwtToken);
}

public class UserValidationService : IUserValidationService
{
    private readonly IUserRepository _repository;
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public UserValidationService(IUserRepository repository, IUserService userService, ITokenService tokenService)
    {
        _repository = repository;
        _userService = userService;
        _tokenService = tokenService;
    }

    public async Task<ValidationReport> ValidateForRegister(RegisterUserDto userdto)
    {
        var report = new ValidationReport();
        var user = await _repository.Get(userdto.Email);

        if (user is not null)
        {
            report.Message = "email field already exists!";
            report.Success = false;
            report.ErrorCode = ErrorCode.AlreadyExists;
            return report;
        }

        report.Message = "User with this email can be created";
        report.Success = true;
        return report;
    }

    public async Task<(ValidationReport report, User user)> ValidateForLogin(LoginUserDto loginUserDto)
    {
        var report = new ValidationReport();
        var user = await _repository.Get(loginUserDto.Email);

        if (user is null)
        {
            report.Message = "User not found";
            report.Success = false;
            report.ErrorCode = ErrorCode.NotFound;
            return (report, null);
        }

        if (!_userService.VerifyPassword(user.Password, loginUserDto.Password))
        {
            report.Message = "User with this combination not found";
            report.Success = false;
            report.ErrorCode = ErrorCode.NotFound;
            return (report, null);
        }
        else
        {
            report.Message = "User Verified";
            report.Success = true;
            return (report, user);
        }

    }

    public ValidationReport ValidateUser(string? jwtToken)
    {
        var report = new ValidationReport();

        if (jwtToken is null)
        {
            report.Success = false;
            report.Message = "You did not provide a JWT token";
            return report;
        }

        var principal = _tokenService.VerifyToken(jwtToken);

        if (principal is null)
        {
            report.Success = false;
            report.Message = "Invalid token";
            return report;
        }

        if (!principal.IsInRole("admin"))
        {
            report.Success = false;
            report.Message = "User is forbidden for changes";
            return report;
        }
        
        report.Success = true;
        report.Message = "User is authorized for changes";
        return report;
    }
}