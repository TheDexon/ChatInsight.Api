# Подключение pgvector — чеклист

1. **Образ БД с pgvector.** Новый `docker-compose.yml` использует
   `pgvector/pgvector:pg16` (вместо чистого postgres:16). Если контейнер уже был:

   ```bash
   docker compose down          # старый контейнер
   docker compose up -d         # поднимется pgvector-образ
   ```
   Данные в томе сохранятся (том тот же). Если хочешь чисто — `docker compose down -v`.

2. **NuGet-пакеты** — добавь в `ChatInsight.Api.csproj` в ItemGroup с пакетами:
   ```xml
   <PackageReference Include="Pgvector" Version="0.3.0" />
   <PackageReference Include="Pgvector.EntityFrameworkCore" Version="0.2.2" />
   ```
   Потом `dotnet restore`.

3. **Embed-модель Ollama:**
   ```bash
   ollama pull nomic-embed-text
   ```

4. **Миграция** (создаст расширение vector + таблицу MessageEmbeddings):
   ```bash
   dotnet ef migrations add AddPgVector
   dotnet ef database update
   ```
   Расширение `vector` ставится автоматически из миграции (HasPostgresExtension),
   образ pgvector его поддерживает.

5. **Запуск и проверка:**
   - на странице чата блок «Смысловой поиск» → «Построить индекс» (фоном, долго на
     больших чатах — эмбеддинг каждого сообщения) → потом ищи по смыслу.
