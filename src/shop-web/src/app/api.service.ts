import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ConfigService } from './config.service';

export interface Product {
  id: number;
  name: string;
  description: string;
  category: string;
  price: number;
}

export interface OrderItem {
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface Order {
  id: string;
  customerEmail: string;
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed';
  total: number;
  createdAt: string;
  processedAt: string | null;
  items: OrderItem[];
}

export interface CreateOrderRequest {
  customerEmail: string;
  items: { productId: number; quantity: number }[];
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ConfigService);

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.config.catalogApiUrl}/api/products`);
  }

  getOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.config.ordersApiUrl}/api/orders`);
  }

  createOrder(request: CreateOrderRequest): Observable<Order> {
    return this.http.post<Order>(`${this.config.ordersApiUrl}/api/orders`, request);
  }
}
