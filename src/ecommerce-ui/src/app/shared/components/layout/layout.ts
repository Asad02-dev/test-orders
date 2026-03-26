import { Component, inject, signal, computed, ChangeDetectionStrategy, HostListener } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';

interface NavItem {
  icon: string;
  label: string;
  route: string;
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LayoutComponent {
  private readonly cartService = inject(CartService);

  readonly cartItemCount = this.cartService.itemCount;
  readonly sidebarExpanded = signal(false);
  readonly mobileMenuOpen = signal(false);
  readonly userMenuOpen = signal(false);

  readonly navItems: NavItem[] = [
    { icon: '📊', label: 'Dashboard', route: '/dashboard' },
    { icon: '📦', label: 'Products', route: '/catalog' },
    { icon: '🛒', label: 'Cart', route: '/cart' },
    { icon: '📋', label: 'Orders', route: '/orders' },
    { icon: '🏭', label: 'Inventory', route: '/inventory' },
    { icon: '💳', label: 'Payments', route: '/payments' },
    { icon: '🔔', label: 'Notifications', route: '/notifications' },
  ];

  readonly headerNavItems = computed(() => [
    { label: 'Dashboard', route: '/dashboard' },
    { label: 'Catalog', route: '/catalog' },
    { label: 'Orders', route: '/orders' },
    { label: 'Inventory', route: '/inventory' },
  ]);

  toggleSidebar(): void {
    this.sidebarExpanded.update((v) => !v);
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((v) => !v);
  }

  toggleUserMenu(): void {
    this.userMenuOpen.update((v) => !v);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.user-menu-wrapper')) {
      this.closeUserMenu();
    }
  }
}
