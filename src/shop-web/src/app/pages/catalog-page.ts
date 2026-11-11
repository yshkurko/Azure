import { CurrencyPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of, startWith } from 'rxjs';
import { ApiService, Product } from '../api.service';
import { CartService } from '../cart.service';

type State = { loading: true } | { loading: false; products: Product[]; error?: string };

@Component({
  selector: 'app-catalog-page',
  imports: [CurrencyPipe],
  template: `
    <h1>Каталог</h1>
    @if (state().loading) {
      <p>Загрузка…</p>
    } @else {
      @if (error()) {
        <p class="error">Не удалось загрузить каталог: {{ error() }}</p>
      }
      <div class="grid">
        @for (product of products(); track product.id) {
          <div class="card">
            <small>{{ product.category }}</small>
            <h3>{{ product.name }}</h3>
            <p>{{ product.description }}</p>
            <p><strong>{{ product.price | currency: 'EUR' }}</strong></p>
            <button (click)="cart.add(product)">В корзину</button>
          </div>
        }
      </div>
    }
  `
})
export class CatalogPage {
  protected readonly cart = inject(CartService);

  protected readonly state = toSignal(
    inject(ApiService).getProducts().pipe(
      map((products): State => ({ loading: false, products })),
      catchError((err) => of<State>({ loading: false, products: [], error: err.message ?? 'unknown error' })),
      startWith<State>({ loading: true })
    ),
    { requireSync: true }
  );

  protected products(): Product[] {
    const s = this.state();
    return s.loading ? [] : s.products;
  }

  protected error(): string | undefined {
    const s = this.state();
    return s.loading ? undefined : s.error;
  }
}
