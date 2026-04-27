using OrderService.Domain.Entities;

namespace OrderService.Application.Commands;

public interface IOrderCommand
{
    Order ToOrder();
}