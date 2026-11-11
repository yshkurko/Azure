import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CartService } from './cart.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header>
      <strong>Shop</strong>
      <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Каталог</a>
      <a routerLink="/cart" routerLinkActive="active">Корзина ({{ cart.count() }})</a>
      <a routerLink="/orders" routerLinkActive="active">Заказы</a>
    </header>
    <main>
      <router-outlet />
    </main>
  `
})
export class App {
  protected readonly cart = inject(CartService);
}
