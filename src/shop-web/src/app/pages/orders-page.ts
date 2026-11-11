import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap, timer } from 'rxjs';
import { ApiService, Order } from '../api.service';

@Component({
  selector: 'app-orders-page',
  imports: [CurrencyPipe, DatePipe],
  template: `
    <h1>Заказы</h1>
    <p><small>Список обновляется каждые 3 секунды — видно, как Order Processor меняет статус.</small></p>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    <table>
      <thead>
        <tr><th>Создан</th><th>Email</th><th>Сумма</th><th>Статус</th><th>Обработан</th></tr>
      </thead>
      <tbody>
        @for (order of orders(); track order.id) {
          <tr>
            <td>{{ order.createdAt | date: 'dd.MM.yyyy HH:mm:ss' }}</td>
            <td>{{ order.customerEmail }}</td>
            <td>{{ order.total | currency: 'EUR' }}</td>
            <td [class]="'status-' + order.status">{{ order.status }}</td>
            <td>{{ order.processedAt | date: 'HH:mm:ss' }}</td>
          </tr>
        } @empty {
          <tr><td colspan="5">Заказов пока нет.</td></tr>
        }
      </tbody>
    </table>
  `
})
export class OrdersPage {
  protected readonly orders = signal<Order[]>([]);
  protected readonly error = signal<string | null>(null);

  constructor() {
    const api = inject(ApiService);

    timer(0, 3000)
      .pipe(
        switchMap(() => api.getOrders()),
        takeUntilDestroyed(inject(DestroyRef))
      )
      .subscribe({
        next: (orders) => {
          this.orders.set(orders);
          this.error.set(null);
        },
        error: (err) => this.error.set(`Не удалось загрузить заказы: ${err.message ?? err.status}`)
      });
  }
}
