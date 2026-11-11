import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../api.service';
import { CartService } from '../cart.service';

@Component({
  selector: 'app-cart-page',
  imports: [CurrencyPipe],
  template: `
    <h1>Корзина</h1>
    @if (cart.lines().length === 0) {
      <p>Корзина пуста.</p>
    } @else {
      <table>
        <thead>
          <tr><th>Товар</th><th>Цена</th><th>Кол-во</th><th></th></tr>
        </thead>
        <tbody>
          @for (line of cart.lines(); track line.product.id) {
            <tr>
              <td>{{ line.product.name }}</td>
              <td>{{ line.product.price | currency: 'EUR' }}</td>
              <td>{{ line.quantity }}</td>
              <td><button (click)="cart.remove(line.product.id)">Убрать</button></td>
            </tr>
          }
        </tbody>
      </table>
      <p><strong>Итого: {{ cart.total() | currency: 'EUR' }}</strong></p>

      <p>
        <label>
          Email:
          <input type="email" [value]="email()" (input)="email.set($any($event.target).value)" placeholder="you@example.com" />
        </label>
      </p>
      <button [disabled]="submitting() || !email()" (click)="submit()">Оформить заказ</button>
      @if (error()) {
        <p class="error">{{ error() }}</p>
      }
    }
  `
})
export class CartPage {
  protected readonly cart = inject(CartService);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  protected readonly email = signal('');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    this.submitting.set(true);
    this.error.set(null);

    this.api
      .createOrder({
        customerEmail: this.email(),
        items: this.cart.lines().map((l) => ({ productId: l.product.id, quantity: l.quantity }))
      })
      .subscribe({
        next: () => {
          this.cart.clear();
          this.router.navigate(['/orders']);
        },
        error: (err) => {
          this.error.set(`Ошибка оформления заказа: ${err.message ?? err.status}`);
          this.submitting.set(false);
        }
      });
  }
}
