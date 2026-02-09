using FluentAssertions;
using Moq;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using SimpleExample.Application.Services;
using SimpleExample.Domain.Entities;
using Xunit;

namespace SimpleExample.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        // Mock: Email ei ole käytössä
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        UserDto result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikäläinen");
        result.Email.Should().Be("matti@example.com");

        // Varmista että AddAsync kutsuttiin kerran
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "existing@example.com"
        };

        User existingUser = new User("Maija", "Virtanen", "existing@example.com");

        // Mock: Email on jo käytössä!
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*jo olemassa*");

        // Varmista että AddAsync EI kutsuttu
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    // TEHTÄVÄ: Kirjoita itse testit seuraaville:
    // 1. GetByIdAsync - löytyy
    [Fact]
    public async Task GetByIdAsync_WhenUserExist_ShouldReturnUser()
    {
        // Arrange        
        Guid id = Guid.NewGuid();
        User existingUser = new User("Matti", "Meikäläinen", "matti.meikalainen@example.com")
        {
            Id = id
        };

        // Mock: käyttäjä löytyy annetulla id:llä
        _mockRepository
            .Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync(existingUser);

        // Act
        UserDto? result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikäläinen");
        result.Email.Should().Be("matti.meikalainen@example.com");

        _mockRepository.Verify(x => x.GetByIdAsync(id), Times.Once);

    }

    // 2. GetByIdAsync - ei löydy
    [Fact]
    public async Task GetByIdAsync_WhenUserNotFound_ShouldReturnNull()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        // Mock: käyttäjää ei löydy
        _mockRepository
            .Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((User?)null);

        // Act
        UserDto? result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();

        _mockRepository.Verify(x => x.GetByIdAsync(id), Times.Once);
    }

    // 3. GetAllAsync - palauttaa listan
    [Fact]
    public async Task GetAllAsync_ShouldGetList()
    {
        // Arrange
        List<User> users = new List<User>
        {
            new User("Matti", "Meikäläinen", "matti.meikalainen@example.com") { Id = Guid.NewGuid() },
            new User("Maija", "Virtanen", "maija.virtanen@example.com") { Id = Guid.NewGuid() }
        };

        // Mock: repository palauttaa listan käyttäjiä
        _mockRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    // 4. UpdateAsync - onnistuu
    [Fact]
    public async Task UpdateAsync_ShouldSuccess()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        User existingUser = new User("Matti", "Meikäläinen", "matti.meikalainen@example.com")
        { 
            Id = id
        };
        UpdateUserDto dto = new UpdateUserDto
        {
            FirstName = "Maija",
            LastName = "Meikäläinen",
            Email = "matti.meikalainen@example.com"
        };

        // Mock: käyttäjä löytyy päivitettäväksi
        _mockRepository
            .Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync(existingUser);

        // Mock: kun service tallentaa päivityksen, repository palauttaa päivitetyn käyttäjän
        _mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(existingUser);
         // Act
        var result = await _service.UpdateAsync(existingUser.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Maija");

        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);

    }

    // 5. UpdateAsync - käyttäjää ei löydy
    [Fact]
    public async Task UpdateAsync_WhenUserNotFound_ShouldReturnNull()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        var dto = new UpdateUserDto { FirstName = "A", LastName = "B", Email = "a@b.com" };


        // Mock: käyttäjää ei löydy päivityksessä
        _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateAsync(id, dto);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }


    // 6. DeleteAsync - onnistuu
    [Fact]
    public async Task DeleteAsync_WhenUserExists_ShouldReturnTrue()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        // Mock: käyttäjä on olemassa
        _mockRepository
            .Setup(x => x.ExistsAsync(id))
            .ReturnsAsync(true);

        // Act
        bool result = await _service.DeleteAsync(id);

        // Assert
        result.Should().BeTrue();

        _mockRepository.Verify(x => x.DeleteAsync(id), Times.Once);
    }

    // 7. DeleteAsync - käyttäjää ei löydy
    [Fact]
    public async Task DeleteAsync_WhenUserNotFound_ShouldReturnFalse()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        // Mock: käyttäjää ei ole olemassa
        _mockRepository
            .Setup(x => x.ExistsAsync(id))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteAsync(id);

        // Assert
        result.Should().BeFalse();

        _mockRepository.Verify(x => x.DeleteAsync(id), Times.Never);
    }
}