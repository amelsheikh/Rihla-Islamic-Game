// A regular grid of terrain heights in game metres, decoded from the .rhm files the pipeline writes.

export interface GridInfo { cell: number; cols: number; rows: number; x0: number; z0: number; yMin: number; yMax: number }

export class Heightmap {
  readonly w: number;
  readonly h: number;
  constructor(readonly info: GridInfo, readonly data: Float32Array) {
    this.w = info.cols + 1;
    this.h = info.rows + 1;
  }

  /** Decode gzip + per-row delta + 16-bit quantisation. */
  static async decode(info: GridInfo, gz: ArrayBuffer | Uint8Array): Promise<Heightmap> {
    const stream = new Blob([gz as BlobPart]).stream().pipeThrough(new DecompressionStream('gzip'));
    const raw = new Uint16Array(await new Response(stream).arrayBuffer());
    const w = info.cols + 1, out = new Float32Array(raw.length), k = (info.yMax - info.yMin) / 65535;
    for (let r = 0; r <= info.rows; r++) {
      let q = 0;
      for (let c = 0; c < w; c++) {
        q = (q + raw[r * w + c]) & 0xffff;
        out[r * w + c] = info.yMin + q * k;
      }
    }
    return new Heightmap(info, out);
  }

  contains(x: number, z: number, margin = 0): boolean {
    const { x0, z0, cell, cols, rows } = this.info;
    return x >= x0 + margin && z >= z0 + margin && x <= x0 + cols * cell - margin && z <= z0 + rows * cell - margin;
  }

  /** Bilinear height; clamps outside the grid. */
  at(x: number, z: number): number {
    const { x0, z0, cell, cols, rows } = this.info;
    const gx = Math.min(cols - 1e-4, Math.max(0, (x - x0) / cell)), gz = Math.min(rows - 1e-4, Math.max(0, (z - z0) / cell));
    const ix = gx | 0, iz = gz | 0, fx = gx - ix, fz = gz - iz, i = iz * this.w + ix, d = this.data;
    return (d[i] * (1 - fx) + d[i + 1] * fx) * (1 - fz) + (d[i + this.w] * (1 - fx) + d[i + this.w + 1] * fx) * fz;
  }

  /** Unit surface normal, written into `out` as [x, y, z]. */
  normalAt(x: number, z: number, out: [number, number, number], e = this.info.cell): void {
    const dx = (this.at(x + e, z) - this.at(x - e, z)) / (2 * e), dz = (this.at(x, z + e) - this.at(x, z - e)) / (2 * e);
    const l = 1 / Math.hypot(dx, 1, dz);
    out[0] = -dx * l; out[1] = l; out[2] = -dz * l;
  }
}
