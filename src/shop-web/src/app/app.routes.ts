import { Routes } from '@angular/router';
import { CatalogPage } from './pages/catalog-page';
import { CartPage } from './pages/cart-page';
import { OrdersPage } from './pages/orders-page';

export const routes: Routes = [
  { path: '', component: CatalogPage, title: 'Каталог' },
  { path: 'cart', component: CartPage, title: 'Корзина' },
  { path: 'orders', component: OrdersPage, title: 'Заказы' },
  { path: '**', redirectTo: '' }
];
