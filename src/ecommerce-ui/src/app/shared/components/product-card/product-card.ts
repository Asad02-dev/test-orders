import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { CurrencyFormatPipe } from '../../pipes/currency-format.pipe';
import { ProductDto } from '../../../core/models';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CurrencyFormatPipe],
  templateUrl: './product-card.html',
  styleUrl: './product-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductCardComponent {
  readonly product = input.required<ProductDto>();

  readonly addToCart = output<ProductDto>();
  readonly viewDetails = output<ProductDto>();

  onAddToCart(event: MouseEvent): void {
    event.stopPropagation();
    this.addToCart.emit(this.product());
  }

  onViewDetails(): void {
    this.viewDetails.emit(this.product());
  }
}
