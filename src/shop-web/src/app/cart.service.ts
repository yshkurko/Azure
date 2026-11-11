import { Injectable, computed, signal } from '@angular/core';
import { Product } from './api.service';

export interface CartLine {
  product: Product;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly linesSignal = signal<CartLine[]>([]);

  readonly lines = this.linesSignal.asReadonly();
  readonly count = computed(() => this.linesSignal().reduce((sum, l) => sum + l.quantity, 0));
  readonly total = computed(() => this.linesSignal().reduce((sum, l) => sum + l.quantity * l.product.price, 0));

  add(product: Product): void {
    this.linesSignal.update((lines) => {
      const existing = lines.find((l) => l.product.id === product.id);
      return existing
        ? lines.map((l) => (l === existing ? { ...l, quantity: l.quantity + 1 } : l))
        : [...lines, { product, quantity: 1 }];
    });
  }

  remove(productId: number): void {
    this.linesSignal.update((lines) => lines.filter((l) => l.product.id !== productId));
  }

  clear(): void {
    this.linesSignal.set([]);
  }
}
