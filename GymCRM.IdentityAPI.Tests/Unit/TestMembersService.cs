using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using GymCRM.IdentityAPI.Models;
using GymCRM.IdentityAPI.Models.DTOs;
using GymCRM.IdentityAPI.Models.Enums;
using GymCRM.IdentityAPI.Models.Exceptions;
using GymCRM.IdentityAPI.Models.Interface;
using GymCRM.IdentityAPI.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using EntityMember = GymCRM.IdentityAPI.Models.Entities.Member;

namespace GymCRM.IdentityAPI.Tests.Unit;

public class TestMembersService
{
    [Fact]
    public async Task GivenMembersExist_WhenGettingAllUsers_ThenMappedMembersAreReturned()
    {
        // Given
        var existing = CreateExistingMember();
        var repositoryMock = new Mock<IMembersRepository>();
        repositoryMock.Setup(x => x.FetchAll(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { existing });
        var service = CreateMembersService(repository: repositoryMock.Object);

        // When
        var result = await service.GetAllUsersAsync();

        // Then
        result.Should().ContainSingle(m => m.Email == existing.Email);
    }

    [Fact]
    public async Task GivenEmptyGuid_WhenGettingUserByGuid_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateMembersService();

        // When
        Func<Task> act = () => service.GetUserByGuidAsync(Guid.Empty);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenMatchingMember_WhenGettingUserByGuid_ThenMappedMemberIsReturned()
    {
        // Given
        var existing = CreateExistingMember();
        var service = CreateMembersService(repository: CreateRepositoryMock(existing).Object);

        // When
        var result = await service.GetUserByGuidAsync(existing.AccountGuid);

        // Then
        result.Should().NotBeNull();
        result.Email.Should().Be(existing.Email);
    }

    [Fact]
    public async Task GivenNoMatchingMember_WhenGettingUserByGuid_ThenNullIsReturned()
    {
        // Given
        var service = CreateMembersService(repository: CreateRepositoryMock().Object);

        // When
        var result = await service.GetUserByGuidAsync(Guid.NewGuid());

        // Then
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenBlankEmail_WhenGettingUserByEmail_ThenArgumentExceptionIsThrown(string? email)
    {
        // Given
        var service = CreateMembersService();

        // When
        Func<Task> act = () => service.GetUserByEmailAsync(email!);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenMatchingEmail_WhenGettingUserByEmail_ThenMappedMemberIsReturned()
    {
        // Given
        var existing = CreateExistingMember();
        var service = CreateMembersService(repository: CreateRepositoryMock(existing).Object);

        // When
        var result = await service.GetUserByEmailAsync(existing.Email);

        // Then
        result.Should().NotBeNull();
        result.AccountGuid.Should().Be(existing.AccountGuid);
    }

    [Fact]
    public async Task GivenNullInsertMember_WhenUpdatingMember_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateMembersService();

        // When
        Func<Task> act = () => service.UpdateMemberAsync(null!, Guid.NewGuid());

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyAccountGuid_WhenUpdatingMember_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateMembersService();
        var dto = CreateUpdateDto(Guid.Empty);

        // When
        Func<Task> act = () => service.UpdateMemberAsync(dto, Guid.NewGuid());

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenMemberNotFound_WhenUpdatingMember_ThenMemberNotFoundExceptionIsThrown()
    {
        // Given - the catch clause now excludes MemberNotFoundException/MemberAccessDeniedException
        // from the swallow-and-return-false behavior, so a not-found update actually propagates
        // this exception (pins the catch-clause fix).
        var repositoryMock = CreateRepositoryMock();
        var service = CreateMembersService(repository: repositoryMock.Object);
        var dto = CreateUpdateDto(Guid.NewGuid());

        // When
        Func<Task> act = () => service.UpdateMemberAsync(dto, Guid.NewGuid());

        // Then
        await act.Should().ThrowAsync<MemberNotFoundException>();
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenUpdatingMember_ThenMemberAccessDeniedExceptionIsThrown()
    {
        // Given
        var existing = CreateExistingMember();
        var caller = CreateExistingMember(accountType: AccountType.Member);
        var service = CreateMembersService(repository: CreateRepositoryMock(existing, caller).Object);
        var dto = CreateUpdateDto(existing.AccountGuid);

        // When
        Func<Task> act = () => service.UpdateMemberAsync(dto, caller.AccountGuid);

        // Then
        await act.Should().ThrowAsync<MemberAccessDeniedException>();
    }

    [Fact]
    public async Task GivenAdminCaller_WhenUpdatingAnotherMember_ThenUpdateSucceeds()
    {
        // Given
        var existing = CreateExistingMember();
        var admin = CreateExistingMember(accountType: AccountType.Admin);
        var repositoryMock = CreateRepositoryMock(existing, admin);
        var unitOfWorkMock = CreateUnitOfWorkMock(saveResult: true);
        var service = CreateMembersService(repository: repositoryMock.Object, unitOfWork: unitOfWorkMock.Object);
        var dto = CreateUpdateDto(existing.AccountGuid);

        // When
        var result = await service.UpdateMemberAsync(dto, admin.AccountGuid);

        // Then
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GivenBlankOptionalTextFields_WhenUpdatingMember_ThenExistingValuesAreKept()
    {
        // Given
        var existing = CreateExistingMember();
        var (service, repositoryMock, _) = CreateMembersServiceWithExistingMember(existing);
        var dto = CreateUpdateDto(existing.AccountGuid);
        dto.Email = "";
        dto.FirstName = null;
        dto.MiddleName = "";
        dto.LastName = null;
        dto.MobileNumber = "";
        dto.PhoneNumber = null;
        dto.TimeZone = "";

        // When
        var result = await service.UpdateMemberAsync(dto, existing.AccountGuid);

        // Then
        result.Should().BeTrue();
        var captured = CapturedUpdate(repositoryMock);
        captured.Email.Should().Be(existing.Email);
        captured.FirstName.Should().Be(existing.FirstName);
        captured.MiddleName.Should().Be(existing.MiddleName);
        captured.LastName.Should().Be(existing.LastName);
        captured.MobileNumber.Should().Be(existing.MobileNumber);
        captured.PhoneNumber.Should().Be(existing.PhoneNumber);
        captured.TimeZone.Should().Be(existing.TimeZone);
    }

    [Fact]
    public async Task GivenNonBlankTextFields_WhenUpdatingMember_ThenNewValuesOverwriteExisting()
    {
        // Given
        var existing = CreateExistingMember();
        var (service, repositoryMock, _) = CreateMembersServiceWithExistingMember(existing);
        var dto = CreateUpdateDto(existing.AccountGuid);

        // When
        var result = await service.UpdateMemberAsync(dto, existing.AccountGuid);

        // Then
        result.Should().BeTrue();
        var captured = CapturedUpdate(repositoryMock);
        captured.Email.Should().Be(dto.Email);
        captured.FirstName.Should().Be(dto.FirstName);
        captured.MiddleName.Should().Be(dto.MiddleName);
        captured.LastName.Should().Be(dto.LastName);
        captured.MobileNumber.Should().Be(dto.MobileNumber);
        captured.PhoneNumber.Should().Be(dto.PhoneNumber);
        captured.TimeZone.Should().Be(dto.TimeZone);
    }

    [Fact]
    public async Task GivenNullWorkoutGroupIds_WhenUpdatingMember_ThenExistingWorkoutGroupIdsAreKept()
    {
        // Given
        var existing = CreateExistingMember();
        var (service, repositoryMock, _) = CreateMembersServiceWithExistingMember(existing);
        var dto = CreateUpdateDto(existing.AccountGuid);
        dto.WorkoutGroupIds = null;

        // When
        await service.UpdateMemberAsync(dto, existing.AccountGuid);

        // Then
        var captured = CapturedUpdate(repositoryMock);
        captured.WorkoutGroupIds.Should().BeEquivalentTo(existing.WorkoutGroupIds);
    }

    [Fact]
    public async Task GivenEmptyWorkoutGroupIds_WhenUpdatingMember_ThenExistingWorkoutGroupIdsAreKept()
    {
        // Given
        var existing = CreateExistingMember();
        var (service, repositoryMock, _) = CreateMembersServiceWithExistingMember(existing);
        var dto = CreateUpdateDto(existing.AccountGuid);
        dto.WorkoutGroupIds = new List<Guid>();

        // When
        await service.UpdateMemberAsync(dto, existing.AccountGuid);

        // Then
        var captured = CapturedUpdate(repositoryMock);
        captured.WorkoutGroupIds.Should().BeEquivalentTo(existing.WorkoutGroupIds);
    }

    [Fact]
    public async Task GivenAlwaysOverwriteFieldsAreNulledOut_WhenUpdatingMember_ThenExistingValuesAreOverwrittenAnyway()
    {
        // Given - WorkingExperienceInMonths/GymSubscriptionType/PersonalTrainerId/Gender have no
        // blank-falls-back guard in MergeExistingMemberDataWithUpdateData, unlike the text fields
        // above - the incoming value always wins, even if that means zeroing out real existing data.
        var existing = CreateExistingMember();
        var (service, repositoryMock, _) = CreateMembersServiceWithExistingMember(existing);
        var dto = CreateUpdateDto(existing.AccountGuid);
        dto.WorkingExperienceInMonths = null;
        dto.PersonalTrainerId = null;
        dto.GymSubscriptionType = 0;
        dto.Gender = 0;

        // When
        await service.UpdateMemberAsync(dto, existing.AccountGuid);

        // Then
        var captured = CapturedUpdate(repositoryMock);
        captured.WorkingExperienceInMonths.Should().BeNull();
        captured.PersonalTrainerId.Should().BeNull();
        captured.GymSubscriptionType.Should().Be(0);
        captured.Gender.Should().Be(0);
    }

    [Fact]
    public async Task GivenNullDateOfBirthAndHourlyPrice_WhenUpdatingMember_ThenExistingValuesAreKept()
    {
        // Given
        var existing = CreateExistingMember();
        var (service, repositoryMock, _) = CreateMembersServiceWithExistingMember(existing);
        var dto = CreateUpdateDto(existing.AccountGuid);
        dto.DateOfBirth = null;
        dto.HourlyPrice = null;

        // When
        await service.UpdateMemberAsync(dto, existing.AccountGuid);

        // Then
        var captured = CapturedUpdate(repositoryMock);
        captured.DateOfBirth.Should().Be(existing.DateOfBirth);
        captured.HourlyPrice.Should().Be(existing.HourlyPrice);
    }

    [Fact]
    public async Task GivenNonNullDateOfBirthAndHourlyPrice_WhenUpdatingMember_ThenNewValuesOverwriteExisting()
    {
        // Given
        var existing = CreateExistingMember();
        var (service, repositoryMock, _) = CreateMembersServiceWithExistingMember(existing);
        var dto = CreateUpdateDto(existing.AccountGuid);

        // When
        await service.UpdateMemberAsync(dto, existing.AccountGuid);

        // Then
        var captured = CapturedUpdate(repositoryMock);
        captured.DateOfBirth.Should().Be(dto.DateOfBirth);
        captured.HourlyPrice.Should().Be(dto.HourlyPrice);
    }

    [Fact]
    public async Task GivenAnyUpdate_WhenUpdatingMember_ThenPhotoAndPhotoContentTypeAreNeverTouched()
    {
        // Given - the critical regression guard: Photo/PhotoContentType never travel through the
        // update DTO at all, so without MergeExistingMemberDataWithUpdateData's explicit carry-over
        // (MembersService.cs:362-363), every profile save would silently wipe a member's photo.
        var existing = CreateExistingMember();
        existing.Photo = new byte[] { 1, 2, 3, 4 };
        existing.PhotoContentType = "image/png";
        var (service, repositoryMock, _) = CreateMembersServiceWithExistingMember(existing);
        var dto = CreateUpdateDto(existing.AccountGuid);
        dto.FirstName = "SomeUnrelatedChange";

        // When
        await service.UpdateMemberAsync(dto, existing.AccountGuid);

        // Then
        var captured = CapturedUpdate(repositoryMock);
        captured.Photo.Should().BeEquivalentTo(existing.Photo);
        captured.PhotoContentType.Should().Be(existing.PhotoContentType);
    }

    [Fact]
    public async Task GivenValidInsertMember_WhenInsertingMember_ThenTheMissingAutoMapperConfigurationCausesFalseToBeReturned()
    {
        // Given - IdentityModule.ConfigureIdentityMappings never registers
        // CreateMap<InsertMember, Entity.Member>(), so _mapper.Map<Entity.Member>(insertMember)
        // throws an AutoMapperMappingException here, which InsertMemberAsync's catch-all turns
        // into `false`. InsertMember/InsertMemberAsync has no controller action anywhere in the
        // app, so this gap has never surfaced in practice - pinning the actual current behavior.
        var repositoryMock = new Mock<IMembersRepository>();
        var service = CreateMembersService(repository: repositoryMock.Object);
        var insertMember = new InsertMember
        {
            AccountType = AccountType.Member,
            Email = "new.insert@test.com",
            GymSubscriptionType = (int)GymSubscriptionType.Monthly
        };

        // When
        var result = await service.InsertMemberAsync(insertMember);

        // Then
        result.Should().BeFalse();
        repositoryMock.Verify(x => x.Insert(It.IsAny<EntityMember>()), Times.Never);
    }

    [Fact]
    public async Task GivenNullOrEmptyPhotoBytes_WhenUploadingPhoto_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateMembersService();
        var accountGuid = Guid.NewGuid();

        // When
        Func<Task> act = () => service.UploadMemberPhotoAsync(accountGuid, accountGuid, Array.Empty<byte>(), "image/png");

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenOversizedPhoto_WhenUploadingPhoto_ThenPhotoTooLargeExceptionIsThrown()
    {
        // Given
        var existing = CreateExistingMember();
        var service = CreateMembersService(repository: CreateRepositoryMock(existing).Object);
        var oversizedPhoto = new byte[5 * 1024 * 1024 + 1];

        // When
        Func<Task> act = () => service.UploadMemberPhotoAsync(existing.AccountGuid, existing.AccountGuid, oversizedPhoto, "image/png");

        // Then
        await act.Should().ThrowAsync<PhotoTooLargeException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("application/pdf")]
    public async Task GivenDisallowedContentType_WhenUploadingPhoto_ThenInvalidPhotoContentTypeExceptionIsThrown(string? contentType)
    {
        // Given
        var existing = CreateExistingMember();
        var service = CreateMembersService(repository: CreateRepositoryMock(existing).Object);

        // When
        Func<Task> act = () => service.UploadMemberPhotoAsync(existing.AccountGuid, existing.AccountGuid, new byte[] { 1, 2, 3 }, contentType);

        // Then
        await act.Should().ThrowAsync<InvalidPhotoContentTypeException>();
    }

    [Fact]
    public async Task GivenTargetMemberDoesNotExist_WhenUploadingPhoto_ThenMemberNotFoundExceptionIsThrown()
    {
        // Given
        var service = CreateMembersService(repository: CreateRepositoryMock().Object);

        // When
        Func<Task> act = () => service.UploadMemberPhotoAsync(Guid.NewGuid(), Guid.NewGuid(), new byte[] { 1 }, "image/png");

        // Then
        await act.Should().ThrowAsync<MemberNotFoundException>();
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenUploadingPhoto_ThenMemberPhotoAccessDeniedExceptionIsThrown()
    {
        // Given
        var target = CreateExistingMember();
        var caller = CreateExistingMember(accountType: AccountType.Member);
        var service = CreateMembersService(repository: CreateRepositoryMock(target, caller).Object);

        // When
        Func<Task> act = () => service.UploadMemberPhotoAsync(target.AccountGuid, caller.AccountGuid, new byte[] { 1 }, "image/png");

        // Then
        await act.Should().ThrowAsync<MemberPhotoAccessDeniedException>();
    }

    [Fact]
    public async Task GivenSelfUpload_WhenUploadingPhoto_ThenPhotoIsSavedAndTrueIsReturned()
    {
        // Given
        var existing = CreateExistingMember();
        var repositoryMock = CreateRepositoryMock(existing);
        var unitOfWorkMock = CreateUnitOfWorkMock(saveResult: true);
        var service = CreateMembersService(repository: repositoryMock.Object, unitOfWork: unitOfWorkMock.Object);
        var photoBytes = new byte[] { 9, 9, 9 };

        // When
        var result = await service.UploadMemberPhotoAsync(existing.AccountGuid, existing.AccountGuid, photoBytes, "image/jpeg");

        // Then
        result.Should().BeTrue();
        existing.Photo.Should().BeEquivalentTo(photoBytes);
        existing.PhotoContentType.Should().Be("image/jpeg");
        repositoryMock.Verify(x => x.Update(existing), Times.Once);
    }

    [Fact]
    public async Task GivenAdminUploadsOnBehalfOfAnotherMember_WhenUploadingPhoto_ThenPhotoIsSavedAndTrueIsReturned()
    {
        // Given
        var target = CreateExistingMember();
        var admin = CreateExistingMember(accountType: AccountType.Admin);
        var repositoryMock = CreateRepositoryMock(target, admin);
        var unitOfWorkMock = CreateUnitOfWorkMock(saveResult: true);
        var service = CreateMembersService(repository: repositoryMock.Object, unitOfWork: unitOfWorkMock.Object);

        // When
        var result = await service.UploadMemberPhotoAsync(target.AccountGuid, admin.AccountGuid, new byte[] { 5 }, "image/webp");

        // Then
        result.Should().BeTrue();
        target.PhotoContentType.Should().Be("image/webp");
    }

    [Fact]
    public async Task GivenMemberDoesNotExist_WhenGettingPhoto_ThenMemberNotFoundExceptionIsThrown()
    {
        // Given
        var service = CreateMembersService(repository: CreateRepositoryMock().Object);

        // When
        Func<Task> act = () => service.GetMemberPhotoAsync(Guid.NewGuid());

        // Then
        await act.Should().ThrowAsync<MemberNotFoundException>();
    }

    [Fact]
    public async Task GivenMemberHasNoPhoto_WhenGettingPhoto_ThenNullIsReturned()
    {
        // Given
        var existing = CreateExistingMember();
        existing.Photo = null;
        existing.PhotoContentType = null;
        var service = CreateMembersService(repository: CreateRepositoryMock(existing).Object);

        // When
        var result = await service.GetMemberPhotoAsync(existing.AccountGuid);

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public async Task GivenMemberHasPhoto_WhenGettingPhoto_ThenBytesAndContentTypeAreReturned()
    {
        // Given
        var existing = CreateExistingMember();
        existing.Photo = new byte[] { 7, 7, 7 };
        existing.PhotoContentType = "image/png";
        var service = CreateMembersService(repository: CreateRepositoryMock(existing).Object);

        // When
        var result = await service.GetMemberPhotoAsync(existing.AccountGuid);

        // Then
        result.Should().NotBeNull();
        result!.Value.Bytes.Should().BeEquivalentTo(existing.Photo);
        result.Value.ContentType.Should().Be(existing.PhotoContentType);
    }

    [Fact]
    public async Task GivenTargetMemberDoesNotExist_WhenDeletingPhoto_ThenMemberNotFoundExceptionIsThrown()
    {
        // Given
        var service = CreateMembersService(repository: CreateRepositoryMock().Object);

        // When
        Func<Task> act = () => service.DeleteMemberPhotoAsync(Guid.NewGuid(), Guid.NewGuid());

        // Then
        await act.Should().ThrowAsync<MemberNotFoundException>();
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenDeletingPhoto_ThenMemberPhotoAccessDeniedExceptionIsThrown()
    {
        // Given
        var target = CreateExistingMember();
        var caller = CreateExistingMember(accountType: AccountType.Member);
        var service = CreateMembersService(repository: CreateRepositoryMock(target, caller).Object);

        // When
        Func<Task> act = () => service.DeleteMemberPhotoAsync(target.AccountGuid, caller.AccountGuid);

        // Then
        await act.Should().ThrowAsync<MemberPhotoAccessDeniedException>();
    }

    [Fact]
    public async Task GivenSelfDelete_WhenDeletingPhoto_ThenPhotoFieldsAreClearedAndTrueIsReturned()
    {
        // Given
        var existing = CreateExistingMember();
        existing.Photo = new byte[] { 3, 3, 3 };
        existing.PhotoContentType = "image/png";
        var unitOfWorkMock = CreateUnitOfWorkMock(saveResult: true);
        var service = CreateMembersService(repository: CreateRepositoryMock(existing).Object, unitOfWork: unitOfWorkMock.Object);

        // When
        var result = await service.DeleteMemberPhotoAsync(existing.AccountGuid, existing.AccountGuid);

        // Then
        result.Should().BeTrue();
        existing.Photo.Should().BeNull();
        existing.PhotoContentType.Should().BeNull();
    }

    private static EntityMember CapturedUpdate(Mock<IMembersRepository> repositoryMock)
    {
        var invocation = repositoryMock.Invocations.Single(i => i.Method.Name == nameof(IMembersRepository.Update));
        return (EntityMember)invocation.Arguments[0];
    }

    private static (MembersService service, Mock<IMembersRepository> repositoryMock, Mock<IUnitOfWork> unitOfWorkMock)
        CreateMembersServiceWithExistingMember(EntityMember existingMember)
    {
        var repositoryMock = CreateRepositoryMock(existingMember);
        var unitOfWorkMock = CreateUnitOfWorkMock(saveResult: true);
        var service = CreateMembersService(repository: repositoryMock.Object, unitOfWork: unitOfWorkMock.Object);

        return (service, repositoryMock, unitOfWorkMock);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(bool saveResult)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveResult);

        return unitOfWorkMock;
    }

    // Backs FetchByCondition with an in-memory list and actually compiles/applies the predicate
    // expression against it, so tests exercising both "does the target exist" and "does the
    // caller exist" lookups (GetMemberForPhotoActionAsync issues two separate FetchByCondition
    // calls with different predicates) get realistic, distinguishable results.
    private static Mock<IMembersRepository> CreateRepositoryMock(params EntityMember[] members)
    {
        var backingList = members.ToList();
        var repositoryMock = new Mock<IMembersRepository>();
        repositoryMock
            .Setup(x => x.FetchByCondition(It.IsAny<Expression<Func<EntityMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<EntityMember, bool>> expression, CancellationToken _) =>
                backingList.Where(expression.Compile()).ToList());

        return repositoryMock;
    }

    private static EntityMember CreateExistingMember(AccountType accountType = AccountType.Member) => new()
    {
        Id = Guid.CreateVersion7(),
        AccountGuid = Guid.NewGuid(),
        Email = "existing@test.com",
        FirstName = "ExistingFirst",
        MiddleName = "ExistingMiddle",
        LastName = "ExistingLast",
        MobileNumber = "111",
        PhoneNumber = "222",
        TimeZone = "Europe/Zagreb",
        AccountType = (int)accountType,
        WorkingExperienceInMonths = 5,
        GymSubscriptionType = (int)GymSubscriptionType.Monthly,
        PersonalTrainerId = Guid.NewGuid(),
        Gender = (int)Gender.Male,
        WorkoutGroupIds = new List<Guid> { Guid.NewGuid() },
        DateOfBirth = new DateOnly(1990, 1, 1),
        HourlyPrice = 30m,
        DateModified = DateTime.UtcNow.AddDays(-1)
    };

    private static Member CreateUpdateDto(Guid accountGuid) => new()
    {
        AccountGuid = accountGuid,
        Email = "new@test.com",
        FirstName = "NewFirst",
        MiddleName = "NewMiddle",
        LastName = "NewLast",
        MobileNumber = "333",
        PhoneNumber = "444",
        TimeZone = "America/New_York",
        AccountType = AccountType.PersonalTrainer,
        WorkingExperienceInMonths = 12,
        GymSubscriptionType = (int)GymSubscriptionType.Yearly,
        PersonalTrainerId = Guid.NewGuid(),
        Gender = (int)Gender.Female,
        WorkoutGroupIds = new List<Guid> { Guid.NewGuid() },
        DateOfBirth = new DateOnly(1995, 6, 1),
        HourlyPrice = 50m
    };

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(IdentityModule.ConfigureIdentityMappings);

        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static MembersService CreateMembersService(
        IUnitOfWork? unitOfWork = null,
        IMembersRepository? repository = null,
        IMapper? mapper = null,
        ILogger? logger = null) =>
        new(
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            repository ?? Mock.Of<IMembersRepository>(),
            mapper ?? CreateMapper(),
            logger ?? Mock.Of<ILogger>());
}
